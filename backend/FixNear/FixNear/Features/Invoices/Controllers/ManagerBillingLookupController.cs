using System.Security.Claims;
using FixNear.Enums;
using FixNear.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Invoices.Controllers;

/// <summary>
/// Shop can read its invoice and payment status. It cannot create or confirm customer payments.
/// </summary>
[ApiController]
[Authorize(Roles = "2")]
[Route("api/shops/{shopId:int}/repair-requests/{repairRequestId:int}/billing")]
public sealed class ManagerBillingLookupController(FixNearDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(int shopId, int repairRequestId)
    {
        if (shopId <= 0 || repairRequestId <= 0)
            return BadRequest(new { message = "Mã cửa hàng hoặc yêu cầu không hợp lệ." });

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var allowed = await db.ShopMember.AsNoTracking().AnyAsync(sm =>
            sm.ShopId == shopId && sm.Manager.UserId == userId &&
            sm.Status == ShopMemberStatus.Active && sm.LeftAt == null &&
            (sm.ShopRole == ShopRole.Owner || sm.ShopRole == ShopRole.Manager));
        if (!allowed) return Forbid();

        var belongsToShop = await db.RepairRequest.AsNoTracking().AnyAsync(r =>
            r.RepairRequestId == repairRequestId && r.ShopId == shopId);
        if (!belongsToShop)
            return NotFound(new { message = "Không tìm thấy yêu cầu sửa chữa của Shop." });

        var invoice = await db.Invoice.AsNoTracking()
            .Where(i => i.ShopId == shopId && i.RepairRequestId == repairRequestId)
            .Select(i => new
            {
                i.InvoiceId,
                i.InvoiceNumber,
                i.TotalAmount,
                i.Status,
                i.IssuedAt,
                i.PaidAt,
                i.PaymentReference,
                Details = i.InvoiceDetail.OrderBy(d => d.InvoiceDetailId)
                    .Select(d => new { d.Description, d.Quantity, d.UnitPrice, d.Amount }).ToList(),
                Payment = i.Payment.OrderByDescending(p => p.PaymentId)
                    .Select(p => new
                    {
                        p.PaymentId,
                        p.PaymentMethod,
                        p.Amount,
                        p.Status,
                        p.CreatedAt,
                        p.PaidAt
                    }).FirstOrDefault()
            }).FirstOrDefaultAsync();

        // No invoice yet is a valid state, so return 200 with null data.
        return Ok(new { data = invoice });
    }
}
