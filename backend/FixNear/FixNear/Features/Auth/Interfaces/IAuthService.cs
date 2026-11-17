using FixNear.Features.Auth.DTOs;

namespace FixNear.Features.Auth.Interfaces
{
    public interface IAuthService
    {
        Task RegisterAsync(RegisterRequestDto request);
        Task VerifyEmailAsync(VerifyEmailRequestDto request);
        Task ResendOtpAsync(ResendOtpRequestDto request);
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    }
}
