using FixNear.Features.Auth.DTOs;

namespace FixNear.Features.Auth.Interfaces
{
    public interface IAuthService
    {
        Task RegisterAsync(RegisterCustomerRequestDto request);
        Task RegisterAsync(RegisterShopRequestDto request);
        Task RegisterAsync(RegisterTechnicianRequestDto request);
        Task VerifyEmailAsync(VerifyEmailRequestDto request);
        Task ResendOtpAsync(ResendOtpRequestDto request);
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    }
}
