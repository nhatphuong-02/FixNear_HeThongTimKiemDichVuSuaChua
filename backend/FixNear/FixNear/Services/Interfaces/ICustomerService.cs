using FixNear.DTOs.Customer;

namespace FixNear.Services.Interfaces
{
    public interface ICustomerService
    {
        //Quản lý thông tin cá nhân
        Task<CustomerProfileResponseDto> GetCustomerProfileAsync(int userId);
        Task UpdateProfileAsync(int userId, UpdateProfileRequestDto request);

        //Quản lý địa chỉ
        Task<List<AddressResponseDto>> GetAddressesAsync(int userId);
        Task<AddressResponseDto> CreateAddressesAsync(int userId, AddressRequestDto request);
        Task<AddressResponseDto> ReplaceAddressAsync(int userId, int addressId, AddressRequestDto request);
        Task SetDefaultAddressAsync(int userId, int addressId);
    }
}
