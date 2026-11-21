using System.Security.Claims;
using FixNear.Infrastructure.Persistence;
using FixNear.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Quotes.Controllers;

[ApiController]
[Authorize(Roles = "2")]
[Route("api/shops/{shopId:int}/repairs/{repairRequestId:int}/quotes")]
public sealed class ManagerQuoteListController(FixNearDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(int shopId, int repairRequestId, int page = 1, int pageSize = 10)
    {
        if (shopId <= 0 || repairRequestId <= 0 || page < 1 || pageSize is < 1 or > 50)
            return BadRequest(new { message = "Tham số không hợp lệ." });
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var permitted = await db.ShopMember.AsNoTracking().AnyAsync(sm =>
            sm.ShopId == shopId && sm.Manager.UserId == userId &&
            sm.Status == ShopMemberStatus.Active && sm.LeftAt == null &&
            (sm.ShopRole == ShopRole.Owner || sm.ShopRole == ShopRole.Manager));
        if (!permitted) return Forbid();

        bool exists = await db.RepairRequest.AsNoTracking().AnyAsync(r =>
            r.ShopId == shopId && r.RepairRequestId == repairRequestId);
        if (!exists) return NotFound();

        var query = db.Quote.AsNoTracking().Where(q => q.RepairRequestId == repairRequestId);
        int total = await query.CountAsync();
        var data = await query.OrderByDescending(q => q.CreatedAt).ThenByDescending(q => q.QuoteId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(q => new
            {
                q.QuoteId, q.QuoteNumber, q.Status, q.TotalAmount,
                q.IsSupplemental, q.ValidUntil, q.CreatedAt
            }).ToListAsync();
        return Ok(new { page, pageSize, total, totalPages = (total + pageSize - 1) / pageSize, data });
    }
}
