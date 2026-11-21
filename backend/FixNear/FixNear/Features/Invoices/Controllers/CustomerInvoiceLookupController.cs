using System.Security.Claims;
using FixNear.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Invoices.Controllers;

// This is a read-only lookup. Existing GET /api/invoices/{id} remains authoritative.
[ApiController]
[Authorize(Roles = "1")]
[Route("api/customers/me/repair-requests")]
public sealed class CustomerInvoiceLookupController(FixNearDbContext db) : ControllerBase
{
    [HttpGet("{repairRequestId:int}/invoice")]
    public async Task<IActionResult> GetInvoiceForRepair(int repairRequestId)
    {
        if (repairRequestId <= 0) return BadRequest(new { message = "Mã yêu cầu không hợp lệ" });
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        // Returning invoice/payment IDs only for the owner prevents cross-customer access.
        var invoice = await db.Invoice.AsNoTracking()
            .Where(i => i.RepairRequestId == repairRequestId && i.Customer.UserId == userId)
            .OrderByDescending(i => i.InvoiceId)
            .Select(i => new
            {
                i.InvoiceId,
                i.InvoiceNumber,
                i.TotalAmount,
                i.Status,
                i.PaidAt,
                PaymentId = i.Payment.OrderByDescending(p => p.PaymentId)
                    .Select(p => (int?)p.PaymentId).FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (invoice == null) return NotFound(new { message = "Yêu cầu chưa có hóa đơn" });
        return Ok(new { data = invoice });
    }
}
