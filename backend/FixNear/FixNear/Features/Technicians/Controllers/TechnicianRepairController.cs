using FixNear.Domain.Enums;
using FixNear.Features.Technicians.DTOs;
using FixNear.Features.Technicians.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FixNear.Features.Technicians.Controllers;

[ApiController]
[Authorize(Roles = "3")]
[Route("api/technician/repairs")]
public sealed class TechnicianRepairController : ControllerBase
{
    private readonly ITechnicianRepairService _service;

    public TechnicianRepairController(
        ITechnicianRepairService service)
    {
        _service = service;
    }

    // Lấy UserId của Technician từ JWT
    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(value, out int userId)
            ? userId : null;
    }

    // Bắt đầu khảo sát
    [HttpPost("{repairRequestId:int}/start-survey")]
    public async Task<IActionResult> StartSurvey(
        int repairRequestId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            await _service.StartSurveyAsync(
                userId.Value, repairRequestId);

            return Ok(new
            {
                message = "Đã bắt đầu khảo sát.",
                repairRequestId,
                status = 5
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Hoàn thành khảo sát và lưu báo cáo
    [HttpPost("{repairRequestId:int}/survey")]
    public async Task<IActionResult> CompleteSurvey(
        int repairRequestId,
        [FromBody] CreateSurveyReportDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var reportId = await _service
                .CompleteSurveyAsync(
                    userId.Value, repairRequestId, dto);

            return StatusCode(201, new
            {
                message = "Lưu báo cáo khảo sát thành công.",
                repairRequestId,
                repairReportId = reportId
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }


    // Bắt đầu sửa chữa
    [HttpPost("{repairRequestId:int}/start-repair")]
    public async Task<IActionResult> StartRepair(
        int repairRequestId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            await _service.StartRepairAsync(
                userId.Value, repairRequestId);

            return Ok(new
            {
                message = "Bắt đầu sửa chữa thành công.",
                repairRequestId,
                status = (int)RepairRequestStatus.Repairing
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Ghi nhận linh kiện sử dụng
    [HttpPost("{repairRequestId:int}/parts")]
    public async Task<IActionResult> RecordPartUsage(
        int repairRequestId,
        [FromBody] CreateRepairPartUsageDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var usageId = await _service.RecordPartUsageAsync(
                userId.Value, repairRequestId, dto);

            return StatusCode(201, new
            {
                message = "Đã ghi nhận linh kiện sử dụng.",
                repairRequestId,
                repairPartUsageId = usageId
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Hoàn thành sửa chữa
    [HttpPost("{repairRequestId:int}/complete")]
    public async Task<IActionResult> CompleteRepair(
        int repairRequestId,
        [FromBody] CompleteRepairDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            await _service.CompleteRepairAsync(
                userId.Value, repairRequestId, dto);

            return Ok(new
            {
                message = "Hoàn thành sửa chữa thành công.",
                repairRequestId,
                status = (int)RepairRequestStatus.Completed
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

}
