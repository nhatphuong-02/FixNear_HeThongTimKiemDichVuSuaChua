
using System.Security.Claims;
using FixNear.Domain.Enums;
using FixNear.Domain.Models.Shops;
using FixNear.Enums;
using FixNear.Features.Shops.DTOs;
using FixNear.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Shops.Controllers;

[ApiController]
[Authorize(Roles = "2,3")]
[Route("api/shops/{shopId:int}/repair-items")]
public sealed class ShopRepairItemController : ControllerBase
{
    private readonly FixNearDbContext _db;

    public ShopRepairItemController(FixNearDbContext db)
    {
        _db = db;
    }

    private int? CurrentUserId()
    {
        var value = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(value, out var id)
            ? id
            : null;
    }

    // Owner hoặc Manager đang hoạt động
    private Task<bool> CanManageAsync(int shopId, int userId)
    {
        return _db.ShopMember.AsNoTracking().AnyAsync(sm =>
            sm.ShopId == shopId &&
            sm.Manager.UserId == userId &&
            sm.Status == ShopMemberStatus.Active &&
            sm.LeftAt == null &&
            (sm.ShopRole == ShopRole.Owner ||
             sm.ShopRole == ShopRole.Manager));
    }

    // Technician đang hợp tác tại Shop
    private Task<bool> IsShopTechnicianAsync(
        int shopId, int userId)
    {
        return _db.TechnicianShop.AsNoTracking().AnyAsync(ts =>
            ts.ShopId == shopId &&
            ts.Technician.UserId == userId &&
            ts.Status == TechnicianShopStatus.Active &&
            ts.LeftAt == null);
    }

    // Danh mục Item chung để Shop lựa chọn
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

        if (!userId.HasValue)
            return Unauthorized();

        if (shopId <= 0 || !ValidPage(page, pageSize))
            return BadRequest(new
            {
                message = "Tham số phân trang không hợp lệ."
            });

        if (!await CanManageAsync(shopId, userId.Value))
            return Forbid();

        keyword = keyword?.Trim();

        if (keyword?.Length > 100)
            return BadRequest(new
            {
                message = "Từ khóa quá dài."
            });

        var query = _db.RepairItem
            .AsNoTracking()
            .Where(i => i.IsActive);

        if (added.HasValue)
        {
            query = query.Where(i =>
                _db.ShopRepairItem.Any(si =>
                    si.ShopId == shopId &&
                    si.RepairItemId == i.RepairItemId)
                == added.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            string pattern = ToLikePattern(keyword);

            query = query.Where(i =>
                EF.Functions.Like(i.ItemCode, pattern) ||
                EF.Functions.Like(i.ItemName, pattern) ||
                (i.Description != null &&
                 EF.Functions.Like(i.Description, pattern)));
        }

        int total = await query.CountAsync();

        var data = await query
            .OrderBy(i => i.ItemName)
            .ThenBy(i => i.RepairItemId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new
            {
                i.RepairItemId,
                i.ItemCode,
                i.ItemName,
                i.Description,
                i.Unit,
                i.DefaultPrice,

                ShopItem = _db.ShopRepairItem
                    .Where(si =>
                        si.ShopId == shopId &&
                        si.RepairItemId == i.RepairItemId)
                    .Select(si => new
                    {
                        si.ShopRepairItemId,
                        si.Price,
                        si.IsActive
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(PageResult(page, pageSize, total, data));
    }

    // Item đã được Shop cấu hình
    [HttpGet]
    public async Task<IActionResult> GetShopItems(
        int shopId,
        [FromQuery] string? keyword,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = CurrentUserId();

        if (!userId.HasValue)
            return Unauthorized();

        if (shopId <= 0 || !ValidPage(page, pageSize))
            return BadRequest(new
            {
                message = "Tham số phân trang không hợp lệ."
            });

        bool canManage = await CanManageAsync(
            shopId, userId.Value);

        bool isTechnician = await IsShopTechnicianAsync(
            shopId, userId.Value);

        if (!canManage && !isTechnician)
            return Forbid();

        keyword = keyword?.Trim();

        if (keyword?.Length > 100)
            return BadRequest(new
            {
                message = "Từ khóa quá dài."
            });

        var query = _db.ShopRepairItem
            .AsNoTracking()
            .Where(si => si.ShopId == shopId);

        // KTV không được xem Item ngừng hoạt động
        if (!canManage)
            query = query.Where(si =>
                si.IsActive && si.RepairItem.IsActive);

        if (isActive.HasValue)
        {
            query = query.Where(si =>
                si.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = ToLikePattern(keyword);

            query = query.Where(si =>
                EF.Functions.Like(
                    si.RepairItem.ItemCode, pattern) ||
                EF.Functions.Like(
                    si.RepairItem.ItemName, pattern));
        }

        int total = await query.CountAsync();

        var data = await query
            .OrderBy(si => si.RepairItem.ItemName)
            .ThenBy(si => si.ShopRepairItemId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(si => new
            {
                si.ShopRepairItemId,
                si.ShopId,
                si.RepairItemId,
                si.RepairItem.ItemCode,
                si.RepairItem.ItemName,
                si.RepairItem.Description,
                si.RepairItem.Unit,
                si.Price,
                si.IsActive,
                ItemIsActive = si.RepairItem.IsActive,
                si.CreatedAt,
                si.UpdatedAt
            })
            .ToListAsync();

        return Ok(PageResult(page, pageSize, total, data));
    }

    // Shop thêm Item từ danh mục hệ thống
    [HttpPost]
    [Authorize(Roles = "2")]
    public async Task<IActionResult> AddItem(
        int shopId,
        [FromBody] AddShopRepairItemDto request)
    {
        var userId = CurrentUserId();

        if (!userId.HasValue)
            return Unauthorized();

        if (shopId <= 0)
            return BadRequest(new
            {
                message = "ShopId không hợp lệ."
            });

        if (!await CanManageAsync(shopId, userId.Value))
            return Forbid();

        var item = await _db.RepairItem
            .AsNoTracking()
            .FirstOrDefaultAsync(i =>
                i.RepairItemId == request.RepairItemId &&
                i.IsActive);

        if (item == null)
            return NotFound(new
            {
                message = "Item không tồn tại hoặc đã ngừng hoạt động."
            });

        bool exists = await _db.ShopRepairItem
            .AnyAsync(si =>
                si.ShopId == shopId &&
                si.RepairItemId == request.RepairItemId);

        if (exists)
            return Conflict(new
            {
                message = "Shop đã thêm Item này."
            });

        var shopItem = new ShopRepairItem
        {
            ShopId = shopId,
            RepairItemId = request.RepairItemId,
            Price = request.Price,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.ShopRepairItem.Add(shopItem);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Không thể thêm Item. Kiểm tra dữ liệu trùng hoặc ràng buộc."
            });
        }

        return StatusCode(201, new
        {
            message = "Đã thêm Item vào Shop.",
            shopItem.ShopRepairItemId,
            shopItem.ShopId,
            shopItem.RepairItemId,
            shopItem.Price,
            shopItem.IsActive
        });
    }

    // Shop cập nhật giá và trạng thái Item
    [HttpPut("{shopRepairItemId:int}")]
    [Authorize(Roles = "2")]
    public async Task<IActionResult> UpdateItem(
        int shopId,
        int shopRepairItemId,
        [FromBody] UpdateShopRepairItemDto request)
    {
        var userId = CurrentUserId();

        if (!userId.HasValue)
            return Unauthorized();

        if (shopId <= 0 || shopRepairItemId <= 0)
            return BadRequest(new
            {
                message = "ID không hợp lệ."
            });

        if (!await CanManageAsync(shopId, userId.Value))
            return Forbid();

        var shopItem = await _db.ShopRepairItem
            .Include(si => si.RepairItem)
            .FirstOrDefaultAsync(si =>
                si.ShopId == shopId &&
                si.ShopRepairItemId == shopRepairItemId);

        if (shopItem == null)
            return NotFound(new
            {
                message = "Không tìm thấy Item trong Shop."
            });

        if (request.IsActive && !shopItem.RepairItem.IsActive)
            return Conflict(new
            {
                message = "Item hệ thống đã ngừng hoạt động."
            });

        shopItem.Price = request.Price;
        shopItem.IsActive = request.IsActive;
        shopItem.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Cập nhật Item thành công.",
            shopItem.ShopRepairItemId,
            shopItem.ShopId,
            shopItem.RepairItemId,
            shopItem.Price,
            shopItem.IsActive
        });
    }

    private static bool ValidPage(int page, int pageSize)
    {
        return page >= 1 &&
            pageSize >= 1 &&
            pageSize <= 50 &&
            (long)(page - 1) * pageSize <= int.MaxValue;
    }

    private static string ToLikePattern(string keyword)
    {
        return "%" + keyword
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]") + "%";
    }

    private static object PageResult<T>(
        int page, int pageSize, int total, List<T> data)
    {
        return new
        {
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling(
                total / (double)pageSize),
            data
        };
    }
}
