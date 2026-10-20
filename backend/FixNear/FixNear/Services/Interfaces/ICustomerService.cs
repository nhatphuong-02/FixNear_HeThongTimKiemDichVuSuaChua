using FixNear.DTOs.Customer;

namespace FixNear.Services.Interfaces
{
    public interface ICustomerService
    {
        Task<CustomerProfileResponseDto> GetCustomerProfileAsync(int userId);
        Task UpdateProfileAsync(int userId, UpdateProfileRequestDto request);
    }
}
