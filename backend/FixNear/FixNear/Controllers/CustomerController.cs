using FixNear.DTOs.Customer;
using FixNear.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FixNear.Controllers
{
    [ApiController]
    [Route("api/customers/me")]
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly ICustomerService _customerService;
        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetCustomerProfileAsync()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            try
            {
                var result = await _customerService
                    .GetCustomerProfileAsync(userId);

                return Ok(result);
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { message = e.Message });
            }
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfileAsync(UpdateProfileRequestDto request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            try
            {
                await _customerService.UpdateProfileAsync(userId, request);

                return Ok(new
                {
                    message = "Cập nhật thông tin cá nhân thành công"
                });
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { message = e.Message });
            }
            catch (ArgumentException e)
            {
                return BadRequest(new { message = e.Message });
            }
        }
    }
}
