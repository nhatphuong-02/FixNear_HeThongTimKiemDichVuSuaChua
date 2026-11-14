using FixNear.DTOs.Customer;
using FixNear.Enums;
using FixNear.Models;
using FixNear.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Net;

namespace FixNear.Services.Service
{
    public class CustomerService : ICustomerService
    {
        private readonly FixNearDbContext _context;
        public CustomerService(FixNearDbContext context)
        {
            _context = context;
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

                //Trả thông tin địa chỉ mới
                return new AddressResponseDto
                {
                    AddressId = address.AddressId,
                    ProvinceId = address.ProvinceId,
                    ProvinceName = province.ProvinceName,
                    WardId = address.WardId,
                    WardName = ward.WardName,
                    AddressLine = address.AddressLine,
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

                return new AddressResponseDto
                {
                    AddressId = newAddress.AddressId,
                    ProvinceId = newAddress.ProvinceId,
                    ProvinceName = province.ProvinceName,
                    WardId = newAddress.WardId,
                    WardName = ward.WardName,
                    AddressLine = newAddress.AddressLine,
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
    }
}
