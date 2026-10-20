using FixNear.DTOs.Auth;
using FixNear.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FixNear.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        { 
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto request)
        {
            await _authService.RegisterAsync(request);
            return Ok(new
            {
                message = "Đăng ký tài khoản thành công"
            });
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
                { }
            }
            catch (InvalidOperationException e)
            {
                return Unauthorized(new
                {
                    message = e.Message
                });
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _authService.GetCurrentUserAsync(userId);

            return Ok(result);
        }
    }
}
