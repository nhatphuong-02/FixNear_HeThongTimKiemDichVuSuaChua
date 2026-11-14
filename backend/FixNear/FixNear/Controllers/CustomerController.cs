using FixNear.DTOs.Customer;
using FixNear.Models;
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

        //Lấy thông tin cá nhân của người dùng hiện tại
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
                return null;

            return userId;
        }


        [HttpGet("profile")]
        public async Task<IActionResult> GetCustomerProfileAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { message = "Người dùng không hợp lệ" });

            try
            {
                var result = await _customerService
                    .GetCustomerProfileAsync(userId.Value);

                return Ok(result);
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { message = e.Message });
            }
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfileAsync([FromBody] UpdateProfileRequestDto request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { message = "Người dùng không hợp lệ" });

            try
            {
                await _customerService.UpdateProfileAsync(userId.Value, request);

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

        [HttpGet("addresses")]
        public async Task<IActionResult> GetAddressesAsync()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized(new { message = "Người dùng không hợp lệ" });

            var addresses = await _customerService.GetAddressesAsync(userId.Value);

            return Ok(addresses);
        }

        [HttpPost("addresses")]
        public async Task<IActionResult> AddAddressAsync([FromBody] AddressRequestDto request)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized(new { message = "Người dùng không hợp lệ" });

            try
            {
                var result = await _customerService.CreateAddressesAsync(userId.Value, request);

                return CreatedAtAction(nameof(GetAddressesAsync), result);
            }
            catch(KeyNotFoundException e)
            {
                return NotFound(new { message = e.Message });
            }
            catch (ArgumentException e)
            {
                return BadRequest(new { message = e.Message });
            }
        }

        [HttpPut("addresses/{addressId:int}")]
        public async Task<IActionResult> ReplaceAddressAsync(int addressId, [FromBody] AddressRequestDto request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { message = "Người dùng không hợp lệ" });

            try
            {
                var result = await _customerService.ReplaceAddressAsync(userId.Value, addressId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { message = e.Message });
            }
            catch (ArgumentException e)
            {
                return BadRequest(new { message = e.Message });
            }
            catch (InvalidOperationException e)
            {
                return Conflict(new { message = e.Message });
            }
        }

        [HttpPut("addresses/{addressId:int}/default")]
        public async Task<IActionResult> SetDefaultAddressAsync(int addressId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { message = "Người dùng không hợp lệ" });
            
            try
            {
                await _customerService.SetDefaultAddressAsync(userId.Value, addressId);
                return Ok(new { message = "Đặt địa chỉ mặc định thành công" });
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { message = e.Message });
            }
            catch (InvalidOperationException e)
            {
                return Conflict(new { message = e.Message });
            }
        }
    }
}
