using FixNear.DTOs.Customer;
using FixNear.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FixNear.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerController : Controller
    {
        private readonly ICustomerService _customerService;
        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCustomerProfileAsync()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _customerService.GetCustomerProfileAsync(userId);

            return Ok(result);
        }
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateProfileAsync(UpdateProfileRequestDto request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _customerService.UpdateProfileAsync(userId,request);

            return Ok(new
            {
                message = "Cập nhật thông tin cá nhân thành công"
            });
        }
    }
}
