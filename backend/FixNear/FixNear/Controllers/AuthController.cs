using FixNear.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;

namespace FixNear.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        [HttpPost("register")]
        public IActionResult Register(RegisterRequestDto request)
        {

            return Ok(request);
        }
    }
}
