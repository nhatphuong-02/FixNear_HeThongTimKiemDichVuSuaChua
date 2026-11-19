using FixNear.Features.AccountSecurity.DTOs;

namespace FixNear.Features.AccountSecurity.Interfaces;

public interface IAccountSecurityService
{
    // Đổi mật khẩu
    Task RequestChangePasswordOtpAsync(int userId);

    Task ChangePasswordAsync( int userId, ChangePasswordRequestDto request);

    // Đổi email
    Task RequestChangeEmailOtpAsync( int userId, RequestChangeEmailOtpDto request);

    Task ChangeEmailAsync(int userId, ChangeEmailRequestDto request);

    // Đổi số điện thoại
    Task RequestChangePhoneOtpAsync(int userId);

    Task ChangePhoneAsync(int userId, ChangePhoneRequestDto request);
}