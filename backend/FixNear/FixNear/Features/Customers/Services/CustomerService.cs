using FixNear.Domain.Enums;
using FixNear.Domain.Models.Customers;
using FixNear.Domain.Models.Locations;
using FixNear.Domain.Models.Notifications;
using FixNear.Domain.Models.Repairs;
using FixNear.Enums;
using FixNear.Features.Customers.DTOs;
using FixNear.Features.Customers.Interfaces;
using FixNear.Infrastructure.Geocoding;
using FixNear.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Net;

namespace FixNear.Features.Customers.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly FixNearDbContext _context;
        private readonly IAddressLocationService _addressLocationService;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(FixNearDbContext context, IAddressLocationService addressLocationService, ILogger<CustomerService> logger)
        {
            _context = context;
            _addressLocationService = addressLocationService;
            _logger = logger;
        }

        public async Task<CustomerProfileResponseDto> GetCustomerProfileAsync(int userId)
        {

            var customer = await _context.CustomerProfile
                .Include(c => c.User)
                .Include(c => c.Address)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
                throw new KeyNotFoundException("Không tìm thấy hồ sơ");

            //Tìm địa chỉ mặc định
            var defaultAddress = customer.Address
                .FirstOrDefault(a => a.AddressId == customer.DefaultAddressId);

            return new CustomerProfileResponseDto
            {
                CustomerId = customer.CustomerId,
                UserId = customer.UserId,
                AvatarUrl = customer.User.AvatarUrl,
                FullName = customer.User.FullName,
                Phone = customer.User.Phone,
                Email = customer.User.Email,
                DateOfBirth = customer.User.DateOfBirth,
                Gender = customer.User.Gender.HasValue
                    ? (customer.User.Gender.Value ? Gender.Male : Gender.Female)
                    : null,
                RoleType = (RoleType)customer.User.RoleType,
                DefaultAddressId = customer.DefaultAddressId,
                ProvinceId = defaultAddress?.ProvinceId,
                WardId = defaultAddress?.WardId,
                AddressLine = defaultAddress?.AddressLine,
                RecipientName = defaultAddress?.RecipientName,
                RecipientPhone = defaultAddress?.RecipientPhone
            };
        }

        public async Task UpdateProfileAsync(int userId, UpdateProfileRequestDto request)
        {
            var customer = await _context.CustomerProfile
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
                throw new InvalidOperationException("Không tìm thấy hồ sơ");

            // Kiểm tra ngày sinh không được ở tương lai
            if (request.DateOfBirth.HasValue &&
                request.DateOfBirth.Value > DateOnly.FromDateTime(DateTime.UtcNow))
                throw new ArgumentException("Ngày sinh không hợp lệ");


            //Cập nhật thông tin cá nhân

            customer.User.FullName = request.FullName.Trim();
            customer.User.DateOfBirth = request.DateOfBirth;

            customer.User.Gender = request.Gender.HasValue
                ? request.Gender.Value == Gender.Male
                : null;

            customer.User.AvatarUrl = request.AvatarUrl;

            await _context.SaveChangesAsync();
        }

        public async Task<List<AddressResponseDto>> GetAddressesAsync(int userId)
        {
            //Tìm hồ sơ khách hàng
            var customer = await _context.CustomerProfile
                .FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null)
                throw new KeyNotFoundException("Không tìm thấy hồ sơ");

            // Lấy danh sách địa chỉ hiện tại
            var addresses = await _context.Address
                .Where(a => a.CustomerId == customer.CustomerId &&
                       a.OwnerType == (int)RoleType.Customer &&
                       a.SupersededByAddressId == null)
                .OrderByDescending(a => a.AddressId == customer.DefaultAddressId)
                .ThenByDescending(a => a.CreatedAt)
                .Select(a => new AddressResponseDto
                {
                    AddressId = a.AddressId,
                    ProvinceId = a.ProvinceId,
                    ProvinceName = a.Province.ProvinceName,
                    WardId = a.WardId,
                    WardName = a.Ward.WardName,
                    AddressLine = a.AddressLine,
                    Latitude = a.Location != null
                        ? a.Location.Latitude
                        : null,

                    Longitude = a.Location != null
                        ? a.Location.Longitude
                        : null,
                    RecipientName = a.RecipientName,
                    RecipientPhone = a.RecipientPhone,
                    IsDefault = a.AddressId == customer.DefaultAddressId,
                    IsLocked = a.IsLocked,
                    CreatedAt = a.CreatedAt
                }).ToListAsync();

            return addresses;
        }

        public async Task<AddressResponseDto> CreateAddressesAsync(int userId, AddressRequestDto request)
        {
            //Transaction để đảm bảo tính toàn vẹn dữ liệu
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                //Tìm hồ sơ khách hàng
                var customer = await _context.CustomerProfile
                    .FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer == null)
                    throw new KeyNotFoundException("Không tìm thấy hồ sơ");

                //Kiểm tra tỉnh/thành phố
                var province = await _context.Province
                    .FirstOrDefaultAsync(p => p.ProvinceId == request.ProvinceId);
                if (province == null)
                    throw new KeyNotFoundException("Không tìm thấy tỉnh/thành phố");

                //Kiểm tra phường/xã có thuộc tỉnh/thành phố
                var ward = await _context.Ward
                    .FirstOrDefaultAsync(w => w.WardId == request.WardId && w.ProvinceId == request.ProvinceId);
                if (ward == null)
                    throw new ArgumentException("Phường/xã không thuộc tỉnh/thành phố");

                //Tạo địa chỉ mới
                var address = new Address
                {
                    CustomerId = customer.CustomerId,
                    OwnerType = (int)RoleType.Customer,
                    ProvinceId = request.ProvinceId,
                    WardId = request.WardId,
                    AddressLine = request.AddressLine.Trim(),
                    RecipientName = request.RecipientName.Trim(),
                    RecipientPhone = request.RecipientPhone.Trim(),
                    IsLocked = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Address.Add(address);
                await _context.SaveChangesAsync();

                //Nếu là địa chỉ mặc định, cập nhật hồ sơ khách hàng
                bool isDefault = customer.DefaultAddressId == null;

                if (isDefault)
                {
                    customer.DefaultAddressId = address.AddressId;
                    await _context.SaveChangesAsync();
                }

                //Hoàn tất transaction
                await transaction.CommitAsync();

                // Lấy tọa độ địa chỉ
                var coordinates =  await _addressLocationService.SaveLocationAsync(address.AddressId);

                //Trả thông tin địa chỉ mới
                return new AddressResponseDto
                {
                    AddressId = address.AddressId,
                    ProvinceId = address.ProvinceId,
                    ProvinceName = province.ProvinceName,
                    WardId = address.WardId,
                    WardName = ward.WardName,
                    AddressLine = address.AddressLine,
                    Latitude = coordinates?.Latitude,
                    Longitude = coordinates?.Longitude,
                    RecipientName = address.RecipientName,
                    RecipientPhone = address.RecipientPhone,
                    IsDefault = isDefault,
                    IsLocked = address.IsLocked,
                    CreatedAt = address.CreatedAt
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<AddressResponseDto> ReplaceAddressAsync(int userId, int addressId, AddressRequestDto request)
        {
            //Kiểm tra thông tin vào
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.AddressLine) ||
                string.IsNullOrWhiteSpace(request.RecipientName) ||
                string.IsNullOrWhiteSpace(request.RecipientPhone))
            {
                throw new ArgumentException("Thông tin địa chỉ không được để trống");
            }

            //Transaction để đảm bảo tính toàn vẹn dữ liệu
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                //Tìm hồ sơ khách hàng
                var customer = await _context.CustomerProfile
                    .FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer == null)
                    throw new KeyNotFoundException("Không tìm thấy hồ sơ");

                //Tìm địa chỉ cũ thuộc khách hàng hiện tại
                var oldAddress = await _context.Address
                     .FirstOrDefaultAsync(a => a.AddressId == addressId &&
                                               a.CustomerId == customer.CustomerId &&
                                               a.OwnerType == (int)RoleType.Customer);
                if (oldAddress == null)
                    throw new KeyNotFoundException("Không tìm thấy địa chỉ");
                if(oldAddress.IsLocked)
                    throw new InvalidOperationException("Địa chỉ đã bị khóa, không thể thay thế");
                if(oldAddress.SupersededByAddressId != null)
                    throw new InvalidOperationException("Địa chỉ đã bị thay thế, không thể thay thế");
                //Kiểm tra tỉnh/thành phố
                var province = await _context.Province
                    .FirstOrDefaultAsync(p => p.ProvinceId == request.ProvinceId);
                if (province == null)
                    throw new KeyNotFoundException("Không tìm thấy tỉnh/thành phố");


                //Kiểm tra phường/xã có thuộc tỉnh/thành phố
                var ward = await _context.Ward
                    .FirstOrDefaultAsync(w => w.WardId == request.WardId && w.ProvinceId == request.ProvinceId);
                if (ward == null)
                    throw new ArgumentException("Phường/xã không thuộc tỉnh/thành phố");
                //Tạo địa chỉ mới
                var newAddress = new Address
                {
                    CustomerId = customer.CustomerId,
                    OwnerType = (int)RoleType.Customer,
                    ProvinceId = request.ProvinceId,
                    WardId = request.WardId,
                    AddressLine = request.AddressLine.Trim(),
                    RecipientName = request.RecipientName.Trim(),
                    RecipientPhone = request.RecipientPhone.Trim(),
                    IsLocked = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Address.Add(newAddress);
                await _context.SaveChangesAsync();

                //Đánh dấu địa chỉ cũ đã được thay thế
                oldAddress.SupersededByAddressId = newAddress.AddressId;

                //Nếu địa chỉ cũ là mặc định, cập nhật hồ sơ khách hàng
                bool isDefault = oldAddress.AddressId == customer.DefaultAddressId;
                if (isDefault)
                    customer.DefaultAddressId = newAddress.AddressId;

                await _context.SaveChangesAsync();

                //Hoàn tất transaction
                await transaction.CommitAsync();

                // Lấy tọa độ địa chỉ
                var coordinates = await _addressLocationService.SaveLocationAsync(newAddress.AddressId);

                return new AddressResponseDto
                {
                    AddressId = newAddress.AddressId,
                    ProvinceId = newAddress.ProvinceId,
                    ProvinceName = province.ProvinceName,
                    WardId = newAddress.WardId,
                    WardName = ward.WardName,
                    AddressLine = newAddress.AddressLine,
                    Latitude = coordinates?.Latitude,
                    Longitude = coordinates?.Longitude,
                    RecipientName = newAddress.RecipientName,
                    RecipientPhone = newAddress.RecipientPhone,
                    IsDefault = isDefault,
                    IsLocked = newAddress.IsLocked,
                    CreatedAt = newAddress.CreatedAt
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task SetDefaultAddressAsync(int userId, int addressId)
        {
            //Tìm hồ sơ khách hàng
            var customer = await _context.CustomerProfile
                .FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null)
                throw new KeyNotFoundException("Không tìm thấy hồ sơ");

            //Tìm địa chỉ thuộc khách hàng hiện tại
            var address = await _context.Address
                 .FirstOrDefaultAsync(a => a.AddressId == addressId &&
                                           a.CustomerId == customer.CustomerId &&
                                           a.OwnerType == (int)RoleType.Customer);
            if (address == null)
                throw new KeyNotFoundException("Không tìm thấy địa chỉ");

            //Kiểm tra địa chỉ có bị khóa hay không
            if (address.IsLocked)
                throw new InvalidOperationException("Địa chỉ đã bị khóa, không thể đặt làm mặc định");

            //Kiểm tra địa chỉ đã bị thay thế hay chưa
            if (address.SupersededByAddressId != null)
                throw new InvalidOperationException("Địa chỉ đã bị thay thế, không thể đặt làm mặc định");

            //Kiểm tra địa chỉ đã là mặc định hay chưa
            if (customer.DefaultAddressId == address.AddressId)
                throw new InvalidOperationException("Địa chỉ đã là mặc định");

            //Cập nhật địa chỉ mặc định
            customer.DefaultAddressId = address.AddressId;
            await _context.SaveChangesAsync();
        }


        //Tạo yêu cầu sửa chữa

        #region Đặt lịch sửa chữa và thông báo cho Shop

        public async Task<int> CreateRepairRequestAsync(
            int userId,
            CreateRepairRequestDto request)
        {
            if (request == null)
                throw new ArgumentException(
                    "Dữ liệu đặt lịch không hợp lệ.");

            // Kiểm tra Customer đăng nhập
            var customer = await _context.CustomerProfile
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
                throw new KeyNotFoundException(
                    "Không tìm thấy hồ sơ khách hàng.");

            if (customer.User.Status != (int)UserStatus.Active)
                throw new InvalidOperationException(
                    "Tài khoản khách hàng không hoạt động.");

            // Kiểm tra Shop
            var shop = await _context.Shop
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    s.ShopId == request.ShopId &&
                    s.Status == (int)ShopStatus.Active);

            if (shop == null)
                throw new KeyNotFoundException(
                    "Cửa hàng không tồn tại hoặc không hoạt động.");

            // Kiểm tra dịch vụ được Shop cung cấp
            bool validService = await _context.ShopService
                .AsNoTracking()
                .AnyAsync(ss =>
                    ss.ShopId == request.ShopId &&
                    ss.ServiceId == request.ServiceId &&
                    ss.IsActive &&
                    ss.Service.IsActive);

            if (!validService)
                throw new ArgumentException(
                    "Cửa hàng không cung cấp dịch vụ này.");

            // Kiểm tra hình thức sửa chữa
            if (request.RepairMethod != 1 &&
                request.RepairMethod != 2)
                throw new ArgumentException(
                    "Hình thức sửa chữa không hợp lệ.");

            int? addressId = null;

            // 1: Sửa tại nhà
            if (request.RepairMethod == 1)
            {
                if (!request.AddressId.HasValue)
                    throw new ArgumentException(
                        "Vui lòng chọn địa chỉ sửa chữa.");

                bool validAddress = await _context.Address
                    .AsNoTracking()
                    .AnyAsync(a =>
                        a.AddressId == request.AddressId.Value &&
                        a.CustomerId == customer.CustomerId &&
                        a.OwnerType == (int)RoleType.Customer &&
                        a.SupersededByAddressId == null &&
                        !a.IsLocked);

                if (!validAddress)
                    throw new ArgumentException(
                        "Địa chỉ không hợp lệ hoặc không thuộc bạn.");

                addressId = request.AddressId.Value;
            }

            // 2: Sửa tại cửa hàng => AddressId = null

            // Kiểm tra mô tả sự cố
            string description =
                request.Description?.Trim() ?? string.Empty;

            if (description.Length < 10 ||
                description.Length > 2000)
                throw new ArgumentException(
                    "Mô tả sự cố phải từ 10 đến 2000 ký tự.");

            // Kiểm tra ngày giờ mong muốn
            if (!request.PreferredDate.HasValue ||
                !request.PreferredTimeFrom.HasValue ||
                !request.PreferredTimeTo.HasValue)
                throw new ArgumentException(
                    "Vui lòng chọn đầy đủ ngày và giờ sửa chữa.");

            var date = request.PreferredDate.Value;
            var timeFrom = request.PreferredTimeFrom.Value;
            var timeTo = request.PreferredTimeTo.Value;

            if (timeTo <= timeFrom)
                throw new ArgumentException(
                    "Giờ kết thúc phải sau giờ bắt đầu.");

            // Ngày giờ địa phương Việt Nam (UTC+7)
            var vietnamTimeZone =
                TimeZoneInfo.FindSystemTimeZoneById(
                    "SE Asia Standard Time");

            var nowVietnam = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow, vietnamTimeZone);

            var preferredStart = date.ToDateTime(timeFrom);

            if (preferredStart <= nowVietnam)
                throw new ArgumentException(
                    "Thời gian đặt lịch phải ở tương lai.");

            // Kiểm tra người quản lý được nhận thông báo
            var receiverUserIds = await _context.ShopMember
                .AsNoTracking()
                .Where(sm =>
                    sm.ShopId == shop.ShopId &&
                    sm.Status == ShopMemberStatus.Active &&
                    sm.LeftAt == null &&
                    (sm.ShopRole == ShopRole.Owner ||
                     sm.ShopRole == ShopRole.Manager) &&
                    sm.Manager.User.Status == (int)UserStatus.Active)
                .Select(sm => sm.Manager.UserId)
                .Distinct()
                .ToListAsync();

            if (receiverUserIds.Count == 0)
                throw new InvalidOperationException(
                    "Cửa hàng hiện chưa có người quản lý hợp lệ.");

            // Lấy mẫu thông báo đã seed trong DB
            var template = await _context.NotificationTemplate
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.TemplateCode == "REPAIR_REQUEST_CREATED");

            if (template == null)
                throw new InvalidOperationException(
                    "Chưa có template REPAIR_REQUEST_CREATED.");

            // Transaction lưu yêu cầu + dịch vụ + thông báo
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Gắn UserId vào phiên SQL để trigger ghi lịch sử
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC sys.sp_set_session_context @key=N'UserId', @value={userId}");

                var now = DateTime.UtcNow;

                // Tạo yêu cầu sửa chữa
                var repair = new RepairRequest
                {
                    CustomerId = customer.CustomerId,
                    ShopId = shop.ShopId,
                    AddressId = addressId,
                    RepairMethod = request.RepairMethod,
                    Description = description,

                    PreferredDate = date,
                    PreferredTimeFrom = timeFrom,
                    PreferredTimeTo = timeTo,

                    Status = (int)RepairRequestStatus.Pending,
                    Priority = 2,
                    CreatedAt = now
                };

                _context.RepairRequest.Add(repair);
                await _context.SaveChangesAsync();

                // Liên kết dịch vụ với Repair
                var repairService = new RepairRequestService
                {
                    RepairRequestId = repair.RepairRequestId,
                    ServiceId = request.ServiceId,
                    CreatedAt = now
                };

                _context.RepairRequestService.Add(repairService);

                // Tạo nội dung thông báo từ template
                var body = (template.BodyTemplate ?? string.Empty)
                    .Replace(
                        "{CustomerName}",
                        customer.User.FullName)
                    .Replace(
                        "{ShopName}",
                        shop.ShopName)
                    .Replace(
                        "{RequestId}",
                        repair.RepairRequestId.ToString());

                // Một thông báo, nhiều người nhận
                var notification = new Notification
                {
                    TemplateId = template.NotificationTemplateId,
                    Title = template.Title,
                    Body = body,

                    RefType = (int)NotificationRefType.RepairRequest,
                    RefId = repair.RepairRequestId,

                    CreatedAt = now
                };

                foreach (var receiverId in receiverUserIds)
                {
                    notification.UserNotification.Add(
                        new UserNotification
                        {
                            UserId = receiverId,
                            IsRead = false,
                            CreatedAt = now
                        });
                }

                _context.Notification.Add(notification);

                // Lưu dịch vụ và thông báo
                await _context.SaveChangesAsync();

                // Xóa UserId khỏi SQL session
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sys.sp_set_session_context @key=N'UserId', @value=NULL");

                await transaction.CommitAsync();

                return repair.RepairRequestId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                // Dọn context kể cả khi xảy ra lỗi
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sys.sp_set_session_context @key=N'UserId', @value=NULL");
            }
        }

        #endregion


    }
}
