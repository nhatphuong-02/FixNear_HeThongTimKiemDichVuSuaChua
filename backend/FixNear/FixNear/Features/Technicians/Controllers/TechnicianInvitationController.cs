using System.Data;
using System.Security.Claims;
using FixNear.Domain.Enums;
using FixNear.Domain.Models.Technicians;
using FixNear.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Technicians.Controllers;

[ApiController]
[Authorize(Roles = "3")]
[Route("api/technician/me/invitations")]
public sealed class TechnicianInvitationController : ControllerBase
{
    private readonly FixNearDbContext _db;

    public TechnicianInvitationController(FixNearDbContext db)
    {
        _db = db;
    }

    private int? CurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyInvitations([FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();
        if (page < 1 || pageSize < 1 || pageSize > 50)
            return BadRequest(new { message = "page phải >= 1 và pageSize từ 1 đến 50." });

        var technician = await _db.TechnicianProfile.AsNoTracking()
            .Where(x => x.UserId == userId.Value)
            .Select(x => new { x.TechnicianId, x.User.Email })
            .FirstOrDefaultAsync();
        if (technician == null) return NotFound(new { message = "Không tìm thấy Technician." });

        var email = technician.Email.ToLower();
        var query = _db.TechnicianShopInvitation.AsNoTracking()
            .Where(i => i.TechnicianId == technician.TechnicianId &&
                        i.Email.ToLower() == email);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(i => i.CreatedAt)
            .ThenByDescending(i => i.InvitationId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new
            {
                i.InvitationId, i.ShopId, i.Shop.ShopName, i.Email,
                Status = (int)i.Status, i.CreatedAt, i.ExpiresAt,
                i.RespondedAt
            }).ToListAsync();

        return Ok(new { page, pageSize, total,
            totalPages = (total + pageSize - 1) / pageSize, data = items });
    }

    [HttpPost("{invitationId:int}/accept")]
    public async Task<IActionResult> Accept(int invitationId)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var technician = await _db.TechnicianProfile
            .Where(t => t.UserId == userId.Value)
            .Select(t => new { t.TechnicianId, t.User.Email })
            .FirstOrDefaultAsync();
        if (technician == null) return NotFound(new { message = "Không tìm thấy Technician." });

        var invitation = await _db.TechnicianShopInvitation
            .FirstOrDefaultAsync(i => i.InvitationId == invitationId &&
                i.TechnicianId == technician.TechnicianId &&
                i.Email.ToLower() == technician.Email.ToLower());
        if (invitation == null) return NotFound(new { message = "Không tìm thấy lời mời của bạn." });
        if (invitation.Status != TechnicianShopInvitationStatus.Pending)
            return Conflict(new { message = "Lời mời không còn ở trạng thái chờ phản hồi." });

        var now = DateTime.UtcNow;
        if (invitation.ExpiresAt <= now)
        {
            invitation.Status = TechnicianShopInvitationStatus.Expired;
            invitation.RespondedAt = now;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return Conflict(new { message = "Lời mời đã hết hạn." });
        }

        var alreadyActive = await _db.TechnicianShop
            .AnyAsync(ts => ts.TechnicianId == technician.TechnicianId &&
                ts.Status == TechnicianShopStatus.Active && ts.LeftAt == null);
        if (alreadyActive)
            return Conflict(new { message = "Technician đã hợp tác với một Shop. Hãy rời Shop đó trước." });

        var existing = await _db.TechnicianShop
            .Where(ts => ts.TechnicianId == technician.TechnicianId &&
                ts.ShopId == invitation.ShopId)
            .OrderByDescending(ts => ts.JoinedAt)
            .FirstOrDefaultAsync();

        if (existing == null)
        {
            _db.TechnicianShop.Add(new TechnicianShop
            {
                TechnicianId = technician.TechnicianId,
                ShopId = invitation.ShopId,
                Status = TechnicianShopStatus.Active,
                IsPrimary = true,
                JoinedAt = now,
                CreatedAt = now,
                LeftAt = null
            });
        }
        else
        {
            existing.Status = TechnicianShopStatus.Active;
            existing.IsPrimary = true;
            existing.JoinedAt = now;
            existing.LeftAt = null;
        }

        invitation.Status = TechnicianShopInvitationStatus.Accepted;
        invitation.RespondedAt = now;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(new { message = "Đã chấp nhận lời mời tham gia Shop.",
            invitationId, shopId = invitation.ShopId });
    }

    [HttpPost("{invitationId:int}/reject")]
    public async Task<IActionResult> Reject(int invitationId)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();
        var technician = await _db.TechnicianProfile.AsNoTracking()
            .Where(t => t.UserId == userId.Value)
            .Select(t => new { t.TechnicianId, t.User.Email })
            .FirstOrDefaultAsync();
        if (technician == null) return NotFound(new { message = "Không tìm thấy Technician." });

        var invitation = await _db.TechnicianShopInvitation
            .FirstOrDefaultAsync(i => i.InvitationId == invitationId &&
                i.TechnicianId == technician.TechnicianId &&
                i.Email.ToLower() == technician.Email.ToLower());
        if (invitation == null) return NotFound(new { message = "Không tìm thấy lời mời của bạn." });
        if (invitation.Status != TechnicianShopInvitationStatus.Pending)
            return Conflict(new { message = "Lời mời không còn ở trạng thái chờ phản hồi." });

        var now = DateTime.UtcNow;
        invitation.Status = invitation.ExpiresAt <= now
            ? TechnicianShopInvitationStatus.Expired
            : TechnicianShopInvitationStatus.Rejected;
        invitation.RespondedAt = now;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã xử lý lời mời.", invitationId,
            status = (int)invitation.Status });
    }
}
