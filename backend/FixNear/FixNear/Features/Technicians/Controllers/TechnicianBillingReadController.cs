using System.Security.Claims;
using FixNear.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Technicians.Controllers;

/// <summary>Read-only billing status for the technician assigned to a repair.
/// No bank account, payment reference or customer-sensitive billing information is exposed.
/// Technicians cannot create invoices or confirm payment.
/// </summary>
[ApiController]
[Authorize(Roles = "3")]
[Route("api/technician/repairs/{repairRequestId:int}/billing")]
public sealed class TechnicianBillingReadController(FixNearDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(int repairRequestId)
    {
        if (repairRequestId <= 0) return BadRequest(new { message = "Mã yêu cầu không hợp lệ." });
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();

        var authorized = await db.RepairAssignment.AsNoTracking().AnyAsync(a =>
            a.RepairRequestId == repairRequestId && a.Technician.UserId == userId &&
            a.AssignmentStatus == 2);
        if (!authorized) return NotFound(new { message = "Không tìm thấy công việc của bạn." });

        var invoice = await db.Invoice.AsNoTracking()
            .Where(i => i.RepairRequestId == repairRequestId)
            .Select(i => new
            {
                i.InvoiceId,
                i.InvoiceNumber,
                i.RepairRequestId,
                i.IssuedAt,
                i.Status,
                i.TotalAmount,
                i.PaidAt,
                Details = i.InvoiceDetail.OrderBy(d => d.InvoiceDetailId)
                    .Select(d => new { d.Description, d.Quantity, d.UnitPrice, d.Amount }).ToList(),
                Payments = i.Payment.OrderByDescending(p => p.CreatedAt)
                    .Select(p => new
                    {
                        p.PaymentId, p.PaymentMethod, p.Amount,
                        p.Status, p.CreatedAt, p.PaidAt
                    }).ToList()
            }).FirstOrDefaultAsync();

        // Đã có quyền xem công việc nhưng hóa đơn chưa được phát hành:
        // trả 200 với data=null để Mobile phân biệt với lỗi 404 về route/phân quyền.
        return Ok(new { data = invoice });
    }
}
