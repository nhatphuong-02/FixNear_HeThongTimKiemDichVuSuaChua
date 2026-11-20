using System.Security.Claims;
using FixNear.Features.Technicians.DTOs;
using FixNear.Features.Technicians.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixNear.Features.Technicians.Controllers;

[ApiController]
[Authorize(Roles = "3")]
[Route("api/technician/assignments")]
public sealed class TechnicianAssignmentController : ControllerBase
{
    private readonly ITechnicianAssignmentService _service;

    public TechnicianAssignmentController(
        ITechnicianAssignmentService service)
    {
        _service = service;
    }

    // Lấy UserId của kỹ thuật viên từ JWT
    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(value, out int userId)
            ? userId
            : null;
    }

    // Xem danh sách công việc
    [HttpGet]
    public async Task<IActionResult> GetMyAssignments()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var result = await _service
            .GetMyAssignmentsAsync(userId.Value);

        return Ok(new
        {
            message = "Lấy danh sách công việc thành công.",
            data = result
        });
    }

    // Xem chi tiết một công việc
    [HttpGet("{assignmentId:int}")]
    public async Task<IActionResult> GetAssignment(
        int assignmentId)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var result = await _service
            .GetAssignmentDetailAsync(
                userId.Value, assignmentId);

        if (result == null)
            return NotFound(new
            {
                message = "Không tìm thấy công việc."
            });

        return Ok(new
        {
            message = "Lấy chi tiết công việc thành công.",
            data = result
        });
    }

    // Chấp nhận phân công
    [HttpPatch("{assignmentId:int}/accept")]
    public async Task<IActionResult> AcceptAssignment(
        int assignmentId)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        try
        {
            await _service.AcceptAssignmentAsync(
                userId.Value, assignmentId);

            return Ok(new
            {
                message = "Đã chấp nhận công việc thành công."
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
    }

    // Từ chối phân công
    [HttpPatch("{assignmentId:int}/reject")]
    public async Task<IActionResult> RejectAssignment(
        int assignmentId,
        [FromBody] RejectAssignmentDto dto)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            await _service.RejectAssignmentAsync(
                userId.Value,
                assignmentId,
                dto.Reason);

            return Ok(new
            {
                message = "Đã từ chối công việc.",
                reason = dto.Reason
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
