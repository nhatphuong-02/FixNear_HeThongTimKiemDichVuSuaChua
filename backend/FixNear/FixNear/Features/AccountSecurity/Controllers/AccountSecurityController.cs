
using FixNear.Features.AccountSecurity.DTOs;
using FixNear.Features.AccountSecurity.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FixNear.Features.AccountSecurity.Controllers;

[Route("api/account-security")]
[ApiController]
[Authorize]
public class AccountSecurityController : ControllerBase
{
    private readonly IAccountSecurityService _accountSecurityService;

    public AccountSecurityController(
        IAccountSecurityService accountSecurityService)
    {
        _accountSecurityService = accountSecurityService;
    }

    // Lấy UserId từ JWT
    private int GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userId, out var id))
            throw new UnauthorizedAccessException(
                "Không xác định được tài khoản đăng nhập.");

        return id;
    }

    //Đổi mật khẩu

    [HttpPost("password/request-otp")]
    public async Task<IActionResult> RequestChangePasswordOtp()
    {
        await _accountSecurityService.RequestChangePasswordOtpAsync(
            GetCurrentUserId());

        return Ok(new
        {
            message = "Mã OTP đổi mật khẩu đã được gửi đến email của bạn."
        });
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequestDto request)
    {
        await _accountSecurityService.ChangePasswordAsync(
            GetCurrentUserId(),
            request);

        return Ok(new
        {
            message = "Đổi mật khẩu thành công."
        });
    }

    //Đổi email

    [HttpPost("email/request-otp")]
    public async Task<IActionResult> RequestChangeEmailOtp(
        [FromBody] RequestChangeEmailOtpDto request)
    {
        await _accountSecurityService.RequestChangeEmailOtpAsync(
            GetCurrentUserId(),
            request);

        return Ok(new
        {
            message = "Mã OTP đã được gửi đến email mới."
        });
    }

    [HttpPut("email")]
    public async Task<IActionResult> ChangeEmail(
        [FromBody] ChangeEmailRequestDto request)
    {
        await _accountSecurityService.ChangeEmailAsync(
            GetCurrentUserId(),
            request);

        return Ok(new
        {
            message = "Đổi email thành công."
        });
    }

    //Đổi số điện thoại

    [HttpPost("phone/request-otp")]
    public async Task<IActionResult> RequestChangePhoneOtp()
    {
        await _accountSecurityService.RequestChangePhoneOtpAsync(
            GetCurrentUserId());

        return Ok(new
        {
            message = "Mã OTP đã được gửi đến email hiện tại."
        });
    }

    [HttpPut("phone")]
    public async Task<IActionResult> ChangePhone(
        [FromBody] ChangePhoneRequestDto request)
    {
        await _accountSecurityService.ChangePhoneAsync(
            GetCurrentUserId(),
            request);

        return Ok(new
        {
            message = "Đổi số điện thoại thành công."
        });
    }
}
