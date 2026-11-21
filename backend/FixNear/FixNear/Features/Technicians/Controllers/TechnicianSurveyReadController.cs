using System.Security.Claims;
using FixNear.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Technicians.Controllers;

[ApiController]
[Authorize(Roles = "3")]
[Route("api/technician/repairs/{repairRequestId:int}/survey")]
public sealed class TechnicianSurveyReadController(FixNearDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(int repairRequestId)
    {
        if (repairRequestId <= 0) return BadRequest(new { message = "Yêu cầu không hợp lệ" });
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var technicianId = await db.TechnicianProfile.AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => (int?)t.TechnicianId).FirstOrDefaultAsync();
        if (!technicianId.HasValue) return NotFound();

        var allowed = await db.RepairAssignment.AsNoTracking().AnyAsync(a =>
            a.RepairRequestId == repairRequestId &&
            a.TechnicianId == technicianId.Value && a.AssignmentStatus == 2);
        if (!allowed) return NotFound();

        var report = await db.RepairReport.AsNoTracking()
            .Where(r => r.RepairRequestId == repairRequestId &&
                r.TechnicianId == technicianId.Value)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new { r.RepairReportId, r.RepairRequestId, r.Diagnosis, r.Recommendation })
            .FirstOrDefaultAsync();
        if (report == null) return NotFound(new { message = "Chưa có báo cáo khảo sát" });
        return Ok(new { data = report });
    }
}
