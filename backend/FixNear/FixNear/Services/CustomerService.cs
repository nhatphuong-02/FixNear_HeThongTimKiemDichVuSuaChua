using FixNear.DTOs.Customer;
using FixNear.Enums;
using FixNear.Models;
using FixNear.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Services
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
    }
}
