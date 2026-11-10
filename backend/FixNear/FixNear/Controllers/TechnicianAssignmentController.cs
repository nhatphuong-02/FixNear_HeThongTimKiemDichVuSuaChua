
using FixNear.DTOs.Technician;
using FixNear.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FixNear.Controllers;

[ApiController]
[Authorize(Roles = "3")]
[Route("api/technician/assignments")]
public class TechnicianAssignmentController : ControllerBase
{
    private readonly ITechnicianAssignmentService _service;

    public TechnicianAssignmentController(
        ITechnicianAssignmentService service)
    {
        _service = service;
    }

    [HttpPatch("{id:int}/accept")]
    public async Task<IActionResult> Accept(int id)
    {
        var userIdText = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdText, out int userId))
            return Unauthorized();

        var result = await _service.AcceptAsync(id, userId);

        return result switch
        {
            AcceptAssignmentResult.Success =>
                Ok(new { message = "Nhận việc thành công" }),

            AcceptAssignmentResult.NotFound =>
                NotFound(new
                {
                    message = "Không tìm thấy công việc được giao"
                }),

            AcceptAssignmentResult.InvalidStatus =>
                Conflict(new
                {
                    message = "Công việc không ở trạng thái chờ nhận"
                }),

            _ => StatusCode(500)
        };
    }

    // ============================================
    // X02 - KY THUAT VIEN TU CHOI CONG VIEC
    // PATCH /api/technician/assignments/{id}/reject
    // ============================================

    [HttpPatch("{id:int}/reject")]
    public async Task<IActionResult> Reject(
        int id,
        [FromBody] RejectAssignmentDto request)
    {
        // 1. Lay UserId tu JWT
        var userIdText = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdText, out int userId))
        {
            return Unauthorized(new
            {
                message = "Tai khoan khong hop le"
            });
        }

        // 2. Kiem tra ly do tu choi
        if (string.IsNullOrWhiteSpace(request.Reason) ||
            request.Reason.Trim().Length > 500)
        {
            return BadRequest(new
            {
                message = "Ly do tu choi phai tu 1 den 500 ky tu"
            });
        }

        // 3. Goi Service xu ly
        var result = await _service.RejectAsync(
            id,
            userId,
            request.Reason);

        // 4. Tra ket qua HTTP
        return result switch
        {
            RejectAssignmentResult.Success =>
                Ok(new
                {
                    message = "Từ chối công việc thành công"
                }),

            RejectAssignmentResult.NotFound =>
                NotFound(new
                {
                    message = "Không tìm thấy công việc được giao"
                }),

            RejectAssignmentResult.InvalidStatus =>
                Conflict(new
                {
                    message = "Công việc không ở trạng thái chờ phản hồi"
                }),

            RejectAssignmentResult.InvalidReason =>
                BadRequest(new
                {
                    message = "Lý do từ chối không hợp lệ"
                }),

            _ => StatusCode(500)
        };
    }

}
