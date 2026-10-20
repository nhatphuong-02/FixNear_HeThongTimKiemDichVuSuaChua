using FixNear.DTOs.Auth;

namespace FixNear.Services.Interfaces
{
    public interface IAuthService
    {
        Task RegisterAsync(RegisterRequestDto request);
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task<CurrentUserResponseDto> GetCurrentUserAsync(int userId);
    }
}
