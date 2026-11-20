using System.Security.Claims;
using FixNear.Domain.Enums;
using FixNear.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Quotes.Controllers;

[ApiController]
[Authorize(Roles = "1")]
[Route("api/customers/me/repair-requests/{repairRequestId:int}/quotes")]
public sealed class CustomerQuoteListController(FixNearDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(int repairRequestId, int page = 1, int pageSize = 10)
    {
        if (repairRequestId <= 0 || page < 1 || pageSize is < 1 or > 50 ||
            (long)(page - 1) * pageSize > int.MaxValue)
            return BadRequest(new { message = "Tham số không hợp lệ." });

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        // Cả hai trường hợp không tồn tại và không sở hữu đều trả 404.
        var ownsRequest = await db.RepairRequest.AsNoTracking()
            .AnyAsync(r => r.RepairRequestId == repairRequestId && r.Customer.UserId == userId);
        if (!ownsRequest)
            return NotFound(new { message = "Không tìm thấy yêu cầu sửa chữa." });

        // Customer không bao giờ thấy báo giá Draft.
        var query = db.Quote.AsNoTracking()
            .Where(q => q.RepairRequestId == repairRequestId && q.Status != (int)QuoteStatus.Draft);
        var total = await query.CountAsync();
        var data = await query
            .OrderByDescending(q => q.CreatedAt).ThenByDescending(q => q.QuoteId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(q => new
            {
                q.QuoteId, q.RepairRequestId, q.QuoteNumber, q.TotalAmount,
                q.Status, q.IsSupplemental, q.ValidUntil, q.CustomerResponse,
                q.CreatedAt
            })
            .ToListAsync();

        return Ok(new { page, pageSize, total,
            totalPages = (int)Math.Ceiling(total / (double)pageSize), data });
    }
}
