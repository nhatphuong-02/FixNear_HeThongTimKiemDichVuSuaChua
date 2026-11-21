using System.Security.Claims;
using FixNear.Features.Technicians.DTOs;
using FixNear.Features.Technicians.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixNear.Features.Technicians.Controllers;

[ApiController]
[Authorize(Roles = "3")]
[Route("api/technician")]
public sealed class TechnicianController : ControllerBase
{
    private readonly ITechnicianService _service;

    public TechnicianController(ITechnicianService service)
    {
        _service = service;
    }

    // Xác định KTV từ JWT
    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(value, out var id)
            ? id
            : null;
    }

    // Hồ sơ của kỹ thuật viên
    [HttpGet("me/profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var result = await _service.GetProfileAsync(
            userId.Value);

        if (result == null)
            return NotFound(new
            {
                message = "Không tìm thấy hồ sơ kỹ thuật viên."
            });

        return Ok(new
        {
            message = "Lấy hồ sơ thành công.",
            data = result
        });
    }

    // Cập nhật kinh nghiệm, giới thiệu và chuyên môn
    [HttpPut("me/profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateTechnicianProfileRequestDto request)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        try
        {
            var updated = await _service.UpdateProfileAsync(
                userId.Value,
                request);

            if (!updated)
                return NotFound(new
                {
                    message = "Không tìm thấy hồ sơ kỹ thuật viên."
                });

            return Ok(new
            {
                message = "Cập nhật hồ sơ thành công."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // Các cửa hàng mà KTV đang hợp tác
    [HttpGet("me/shops")]
    public async Task<IActionResult> GetMyShops()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var result = await _service.GetMyShopsAsync(
            userId.Value);

        return Ok(new
        {
            message = "Lấy danh sách cửa hàng thành công.",
            data = result
        });
    }

    // Lịch sửa chữa của KTV
    [HttpGet("me/schedules")]
    public async Task<IActionResult> GetSchedules(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        if (from.HasValue && to.HasValue &&
            from.Value > to.Value)
        {
            return BadRequest(new
            {
                message = "Thời gian bắt đầu phải trước thời gian kết thúc."
            });
        }

        // Mặc định lấy lịch trong ngày UTC hiện tại
        // Mobile có thể truyền khoảng ngày cụ thể
        if (!from.HasValue && !to.HasValue)
        {
            from = DateTime.UtcNow.Date;
            to = from.Value.AddDays(1).AddTicks(-1);
        }

        var result = await _service.GetMySchedulesAsync(
            userId.Value,
            from,
            to);

        return Ok(new
        {
            message = "Lấy lịch làm việc thành công.",
            data = result
        });
    }
}