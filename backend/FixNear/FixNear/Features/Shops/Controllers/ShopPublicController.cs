using FixNear.Enums;
using FixNear.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Shops.Controllers;

// Public APIs only: never expose ShopMember, TaxCode, or internal prices.
[ApiController]
[Route("api/shops")]
public sealed class ShopPublicController : ControllerBase
{
    private readonly FixNearDbContext _db;

    public ShopPublicController(FixNearDbContext db)
    {
        _db = db;
    }

    // GET /api/shops/1/public-profile
    [HttpGet("{shopId:int}/public-profile")]
    public async Task<IActionResult> GetPublicProfile(int shopId)
    {
        if (shopId <= 0)
            return BadRequest(new { message = "ShopId không hợp lệ." });

        var shop = await _db.Shop
            .AsNoTracking()
            .Where(s => s.ShopId == shopId &&
                        s.Status == (int)ShopStatus.Active)
            .Select(s => new
            {
                s.ShopId,
                s.ShopName,
                s.Description,
                s.Phone,
                s.Email,
                s.LogoUrl,
                s.RatingAverage,
                s.IsVerified,
                Address = new
                {
                    s.Address.AddressLine,
                    s.Address.Ward.WardName,
                    s.Address.Province.ProvinceName,
                    Latitude = s.Address.Location == null
                        ? (decimal?)null : s.Address.Location.Latitude,
                    Longitude = s.Address.Location == null
                        ? (decimal?)null : s.Address.Location.Longitude
                },
                Images = s.ShopImage
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.DisplayOrder)
                    .ThenBy(i => i.ShopImageId)
                    .Select(i => new
                    {
                        i.ShopImageId,
                        i.ImageUrl,
                        i.Caption,
                        i.IsPrimary,
                        i.DisplayOrder
                    })
                    .ToList(),
                ServiceCount = s.ShopService.Count(ss =>
                    ss.IsActive && ss.Service.IsActive &&
                    ss.Service.Category.IsActive)
            })
            .FirstOrDefaultAsync();

        if (shop == null)
            return NotFound(new { message = "Không tìm thấy Shop đang hoạt động." });

        return Ok(new { data = shop });
    }

    // GET /api/shops/1/public-services?page=1&pageSize=10&keyword=...
    [HttpGet("{shopId:int}/public-services")]
    public async Task<IActionResult> GetPublicServices(
        int shopId,
        [FromQuery] string? keyword = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (shopId <= 0 || page < 1 || pageSize < 1 ||
            pageSize > 50 || (long)(page - 1) * pageSize > int.MaxValue ||
            (categoryId.HasValue && categoryId <= 0))
            return BadRequest(new { message = "Tham số không hợp lệ." });

        keyword = keyword?.Trim();
        if (keyword?.Length > 100)
            return BadRequest(new { message = "Từ khóa tối đa 100 ký tự." });

        var shopExists = await _db.Shop.AsNoTracking().AnyAsync(s =>
            s.ShopId == shopId && s.Status == (int)ShopStatus.Active);
        if (!shopExists)
            return NotFound(new { message = "Không tìm thấy Shop đang hoạt động." });

        var query = _db.ShopService.AsNoTracking()
            .Where(ss => ss.ShopId == shopId && ss.IsActive &&
                         ss.Service.IsActive && ss.Service.Category.IsActive);

        if (categoryId.HasValue)
            query = query.Where(ss => ss.Service.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            string pattern = "%" + keyword.Replace("[", "[[]")
                .Replace("%", "[%]").Replace("_", "[_]") + "%";
            query = query.Where(ss =>
                EF.Functions.Like(ss.Service.ServiceName, pattern) ||
                (ss.Service.Description != null &&
                 EF.Functions.Like(ss.Service.Description, pattern)));
        }

        int total = await query.CountAsync();
        var now = DateTime.UtcNow;

        var data = await query
            .OrderBy(ss => ss.Service.ServiceName)
            .ThenBy(ss => ss.ShopServiceId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ss => new
            {
                ss.ShopServiceId,
                ss.ServiceId,
                ss.Service.ServiceName,
                CategoryId = ss.Service.CategoryId,
                CategoryName = ss.Service.Category.CategoryName,
                ServiceDescription = ss.Service.Description,
                ShopDescription = ss.Description,
                // Publicly show only the current active service price.
                CurrentPrice = ss.ServicePrice
                    .Where(p => p.IsActive && p.EffectiveFrom <= now &&
                        (p.EffectiveTo == null || p.EffectiveTo > now))
                    .OrderByDescending(p => p.EffectiveFrom)
                    .ThenByDescending(p => p.ServicePriceId)
                    .Select(p => new
                    {
                        p.Price,
                        p.MinPrice,
                        p.MaxPrice,
                        p.PriceUnit
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(new
        {
            shopId,
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            data
        });
    }
}
