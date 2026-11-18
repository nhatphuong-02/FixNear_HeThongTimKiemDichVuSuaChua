using FixNear.Features.Auth.DTOs;
using FixNear.Features.Auth.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FixNear.Features.Auth.Controller
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        { 
            _authService = authService;
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequestDto request)
        {
            try
            {
                await _authService.VerifyEmailAsync(request);

                return Ok(new { message = "Xác thực email thành công" });
            }
            catch(InvalidOperationException e)
            {
                return BadRequest(new { message = e.Message });
            }
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequestDto request)
        {
            try
            {
                await _authService.ResendOtpAsync(request);

                return Ok(new {message = "Mã OTP mới đã được gửi đến email của bạn."});
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(new {message = e.Message});
            }
        }

        [HttpPost("register/customer")]
        public async Task<IActionResult> Register([FromBody] RegisterCustomerRequestDto request)
        {
            try
            {
                await _authService.RegisterAsync(request);

                return Ok(new
                {
                    message = "Yêu cầu đăng ký đã được tiếp nhận. Vui lòng kiểm tra email để xác thực tài khoản."
                });
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(new { message = e.Message });
            }
        }

        [HttpPost("register/shop")]
        public async Task<IActionResult> RegisterShop([FromBody] RegisterShopRequestDto request)
        {
            try
            {
                await _authService.RegisterAsync(request);

                return Ok(new
                {
                    message = "Vui lòng kiểm tra email để xác thực tài khoản Shop"
                });
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(new { message = e.Message });
            }
        }

        [HttpPost("register/technician")]
        public async Task<IActionResult> RegisterTechnician([FromBody] RegisterTechnicianRequestDto request)
        {
            try
            {
                await _authService.RegisterAsync(request);

                return Ok(new
                {
                    message = "Vui lòng kiểm tra email để xác thực tài khoản kỹ thuật viên"
                });
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(new { message = e.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);

                return Ok(new
                {
                    message = "Đăng nhập thành công",
                    data = result
                });
            }
            catch (InvalidOperationException e)
            {
                return Unauthorized(new
                {
                    message = e.Message
                });
            }
        }
    }
}
