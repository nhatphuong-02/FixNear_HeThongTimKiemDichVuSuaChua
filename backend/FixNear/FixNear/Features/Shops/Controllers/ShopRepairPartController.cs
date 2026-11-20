
using FixNear.Domain.Enums;
using FixNear.Domain.Models.Shops;
using FixNear.Enums;
using FixNear.Features.Shops.DTOs;
using FixNear.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FixNear.Features.Shops.Controllers;

[ApiController]
[Authorize(Roles = "2,3")]
[Route("api/shops/{shopId:int}/repair-parts")]
public sealed class ShopRepairPartController : ControllerBase
{
    private readonly FixNearDbContext _db;

    public ShopRepairPartController(FixNearDbContext db)
    {
        _db = db;
    }

    private int? CurrentUserId()
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var id) ? id : null;
    }

    private Task<bool> CanManageAsync(int shopId, int userId)
    {
        return _db.ShopMember.AsNoTracking().AnyAsync(m =>
            m.ShopId == shopId &&
            m.Manager.UserId == userId &&
            m.Status == ShopMemberStatus.Active &&
            m.LeftAt == null &&
            (m.ShopRole == ShopRole.Owner ||
             m.ShopRole == ShopRole.Manager));
    }

    private Task<bool> IsTechnicianAsync(int shopId, int userId)
    {
        return _db.TechnicianShop.AsNoTracking().AnyAsync(t =>
            t.ShopId == shopId &&
            t.Technician.UserId == userId &&
            t.Status == TechnicianShopStatus.Active &&
            t.LeftAt == null);
    }

    // Danh mục linh kiện chung để Shop lựa chọn
    [HttpGet("catalog")]
    [Authorize(Roles = "2")]
    public async Task<IActionResult> GetCatalog(
        int shopId,
        [FromQuery] string? keyword,
        [FromQuery] bool? added,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        if (shopId <= 0 || !ValidPage(page, pageSize) ||
            keyword?.Length > 100)
            return BadRequest(new { message = "Tham số không hợp lệ." });

        if (!await CanManageAsync(shopId, userId.Value))
            return Forbid();

        var query = _db.RepairPart.AsNoTracking()
            .Where(p => p.IsActive);

        if (added.HasValue)
            query = query.Where(p =>
                _db.ShopRepairPart.Any(sp =>
                    sp.ShopId == shopId &&
                    sp.RepairPartId == p.RepairPartId)
                == added.Value);

        keyword = keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = Pattern(keyword);
            query = query.Where(p =>
                EF.Functions.Like(p.PartName, pattern) ||
                EF.Functions.Like(p.PartCode, pattern));
        }

        var total = await query.CountAsync();

        var data = await query
            .OrderBy(p => p.PartName)
            .ThenBy(p => p.RepairPartId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.RepairPartId,
                p.PartCode,
                p.PartName,
                p.Description,
                p.Unit,
                p.CurrentPrice,
                ShopPart = _db.ShopRepairPart
                    .Where(sp =>
                        sp.ShopId == shopId &&
                        sp.RepairPartId == p.RepairPartId)
                    .Select(sp => new
                    {
                        sp.ShopRepairPartId,
                        sp.Price,
                        sp.IsActive
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            data
        });
    }

    // Danh sách linh kiện của Shop
    [HttpGet]
    public async Task<IActionResult> GetShopParts(
        int shopId,
        [FromQuery] string? keyword,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        if (shopId <= 0 || !ValidPage(page, pageSize) ||
            keyword?.Length > 100)
            return BadRequest(new { message = "Tham số không hợp lệ." });

        bool manager = await CanManageAsync(shopId, userId.Value);
        bool technician = await IsTechnicianAsync(shopId, userId.Value);

        if (!manager && !technician)
            return Forbid();

        var query = _db.ShopRepairPart.AsNoTracking()
            .Where(sp => sp.ShopId == shopId);

        if (!manager)
            query = query.Where(sp =>
                sp.IsActive && sp.RepairPart.IsActive);

        if (isActive.HasValue)
            query = query.Where(sp =>
                sp.IsActive == isActive.Value);

        keyword = keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = Pattern(keyword);
            query = query.Where(sp =>
                EF.Functions.Like(sp.RepairPart.PartName, pattern) ||
                EF.Functions.Like(sp.RepairPart.PartCode, pattern));
        }

        var total = await query.CountAsync();

        var data = await query
            .OrderBy(sp => sp.RepairPart.PartName)
            .ThenBy(sp => sp.ShopRepairPartId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(sp => new
            {
                sp.ShopRepairPartId,
                sp.ShopId,
                sp.RepairPartId,
                sp.RepairPart.PartCode,
                sp.RepairPart.PartName,
                sp.RepairPart.Description,
                sp.RepairPart.Unit,
                sp.Price,
                sp.IsActive,
                PartIsActive = sp.RepairPart.IsActive
            })
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            data
        });
    }

    // Shop thêm linh kiện và thiết lập giá
    [HttpPost]
    [Authorize(Roles = "2")]
    public async Task<IActionResult> Add(
        int shopId,
        [FromBody] AddShopRepairPartDto request)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        if (shopId <= 0)
            return BadRequest(new { message = "ShopId không hợp lệ." });

        if (!await CanManageAsync(shopId, userId.Value))
            return Forbid();

        var validPart = await _db.RepairPart
            .AnyAsync(p =>
                p.RepairPartId == request.RepairPartId &&
                p.IsActive);

        if (!validPart)
            return NotFound(new
            {
                message = "Linh kiện không tồn tại hoặc ngừng hoạt động."
            });

        var exists = await _db.ShopRepairPart
            .AnyAsync(sp =>
                sp.ShopId == shopId &&
                sp.RepairPartId == request.RepairPartId);

        if (exists)
            return Conflict(new
            {
                message = "Linh kiện đã được thêm vào Shop."
            });

        var part = new ShopRepairPart
        {
            ShopId = shopId,
            RepairPartId = request.RepairPartId,
            Price = request.Price,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.ShopRepairPart.Add(part);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is Microsoft.Data.SqlClient.SqlException sql &&
            (sql.Number == 2601 || sql.Number == 2627))
        {
            return Conflict(new
            {
                message = "Shop đã cấu hình linh kiện này."
            });
        }

        return StatusCode(201, new
        {
            part.ShopRepairPartId,
            part.ShopId,
            part.RepairPartId,
            part.Price,
            part.IsActive
        });
    }

    // Shop cập nhật giá và trạng thái
    [HttpPut("{id:int}")]
    [Authorize(Roles = "2")]
    public async Task<IActionResult> Update(
        int shopId,
        int id,
        [FromBody] UpdateShopRepairPartDto request)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        if (shopId <= 0 || id <= 0)
            return BadRequest(new { message = "ID không hợp lệ." });

        if (!await CanManageAsync(shopId, userId.Value))
            return Forbid();

        var part = await _db.ShopRepairPart
            .Include(sp => sp.RepairPart)
            .FirstOrDefaultAsync(sp =>
                sp.ShopId == shopId &&
                sp.ShopRepairPartId == id);

        if (part == null)
            return NotFound();

        if (request.IsActive && !part.RepairPart.IsActive)
            return Conflict(new
            {
                message = "Linh kiện hệ thống đã ngừng hoạt động."
            });

        part.Price = request.Price;
        part.IsActive = request.IsActive;
        part.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Đã cập nhật giá linh kiện.",
            part.ShopRepairPartId,
            part.Price,
            part.IsActive
        });
    }

    private static bool ValidPage(int page, int size) =>
        page >= 1 && size >= 1 && size <= 50 &&
        (long)(page - 1) * size <= int.MaxValue;

    private static string Pattern(string keyword) =>
        "%" + keyword.Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]") + "%";
}
