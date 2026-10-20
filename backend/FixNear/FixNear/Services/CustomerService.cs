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
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
                throw new InvalidOperationException("Không tìm thấy hồ sơ");

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
                DefaultAddressId = customer.DefaultAddressId
            };
        }

        public async Task UpdateProfileAsync(int userId, UpdateProfileRequestDto request)
        {
            var customer = await _context.CustomerProfile
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
                throw new InvalidOperationException("Không tìm thấy hồ sơ");

            customer.User.FullName = request.FullName;
            customer.User.Phone = request.Phone;
            customer.User.Email = request.Email;
            customer.User.DateOfBirth = request.DateOfBirth;
            if (request.Gender.HasValue)
            {
                customer.User.Gender =
                    request.Gender.Value == Gender.Male;
            }
            customer.User.AvatarUrl = request.AvatarUrl;

            await _context.SaveChangesAsync();
        }
    }
}
