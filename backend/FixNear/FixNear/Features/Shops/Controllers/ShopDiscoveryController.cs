
using FixNear.Enums;
using FixNear.Features.Shops.DTOs;
using FixNear.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Shops.Controllers;

[ApiController]
public sealed class ShopDiscoveryController : ControllerBase
{
    private readonly FixNearDbContext _db;

    public ShopDiscoveryController(FixNearDbContext db)
    {
        _db = db;
    }

    // Tìm tất cả Shop trong bán kính tối đa 10 km
    [HttpGet("api/shops/nearby")]
    public Task<IActionResult> GetNearbyShops(
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        [FromQuery] double radiusKm = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        return SearchAsync(
            latitude, longitude, radiusKm,
            keyword, null, page, pageSize);
    }

    // Tìm Shop trong bán kính có cung cấp dịch vụ cụ thể
    [HttpGet("api/services/{serviceId:int}/shops")]
    public Task<IActionResult> GetNearbyShopsByService(
        int serviceId,
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        [FromQuery] double radiusKm = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        return SearchAsync(
            latitude, longitude, radiusKm,
            keyword, serviceId, page, pageSize);
    }

    // Logic dùng chung cho hai API
    private async Task<IActionResult> SearchAsync(
        double? latitude,
        double? longitude,
        double radiusKm,
        string? keyword,
        int? serviceId,
        int page,
        int pageSize)
    {
        if (!latitude.HasValue ||
            !longitude.HasValue ||
            !double.IsFinite(latitude.Value) ||
            !double.IsFinite(longitude.Value) ||
            latitude.Value < -90 ||
            latitude.Value > 90 ||
            longitude.Value < -180 ||
            longitude.Value > 180)
        {
            return BadRequest(new
            {
                message = "Tọa độ GPS không hợp lệ."
            });
        }

        if (!double.IsFinite(radiusKm) ||
            radiusKm <= 0 || radiusKm > 10)
        {
            return BadRequest(new
            {
                message = "Bán kính phải lớn hơn 0 và tối đa 10 km."
            });
        }

        if (page < 1 || pageSize < 1 ||
            pageSize > 50 ||
            (long)(page - 1) * pageSize > int.MaxValue)
        {
            return BadRequest(new
            {
                message = "Phân trang không hợp lệ."
            });
        }

        if (serviceId.HasValue && serviceId.Value <= 0)
        {
            return BadRequest(new
            {
                message = "ServiceId không hợp lệ."
            });
        }

        keyword = keyword?.Trim();

        if (keyword?.Length > 100)
        {
            return BadRequest(new
            {
                message = "Từ khóa không vượt quá 100 ký tự."
            });
        }

        if (serviceId.HasValue)
        {
            int id = serviceId.Value;

            bool exists = await _db.Service
                .AsNoTracking()
                .AnyAsync(s =>
                    s.ServiceId == id &&
                    s.IsActive &&
                    s.Category.IsActive);

            if (!exists)
                return NotFound(new
                {
                    message =
                        "Dịch vụ không tồn tại hoặc đã ngừng hoạt động."
                });
        }

        double lat = latitude.Value;
        double lon = longitude.Value;

        // Lọc sơ bộ theo vĩ độ ở SQL Server.
        // 110 km/độ là mức bảo thủ để không bỏ sót
        // Shop nằm trong bán kính cần tìm.
        double latDelta = radiusKm / 110.0;

        double minLat = Math.Max(-90, lat - latDelta);
        double maxLat = Math.Min(90, lat + latDelta);

        var query = _db.Shop
            .AsNoTracking()
            .Where(s =>
                s.Status == (int)ShopStatus.Active &&
                s.Address.Location != null &&
                (double)s.Address.Location.Latitude >= minLat &&
                (double)s.Address.Location.Latitude <= maxLat);

        // Chỉ những Shop đang cung cấp dịch vụ được chọn
        if (serviceId.HasValue)
        {
            int id = serviceId.Value;

            query = query.Where(s =>
                s.ShopService.Any(ss =>
                    ss.ServiceId == id &&
                    ss.IsActive &&
                    ss.Service.IsActive &&
                    ss.Service.Category.IsActive));
        }

        // Tìm theo tên Shop, mô tả hoặc dịch vụ
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            string pattern = "%" +
                keyword.Replace("[", "[[]")
                       .Replace("%", "[%]")
                       .Replace("_", "[_]") +
                "%";

            query = query.Where(s =>
                EF.Functions.Like(s.ShopName, pattern) ||
                (s.Description != null &&
                 EF.Functions.Like(
                     s.Description, pattern)) ||
                s.ShopService.Any(ss =>
                    ss.IsActive &&
                    ss.Service.IsActive &&
                    ss.Service.Category.IsActive &&
                    EF.Functions.Like(
                        ss.Service.ServiceName, pattern)));
        }

        // Chỉ truy vấn các trường cần thiết
        var candidates = await query
            .Select(s => new
            {
                s.ShopId,
                s.ShopName,
                s.Description,
                s.Phone,
                s.LogoUrl,
                s.RatingAverage,

                Address =
                    s.Address.AddressLine + ", " +
                    s.Address.Ward.WardName + ", " +
                    s.Address.Province.ProvinceName,

                Latitude =
                    (double)s.Address.Location!.Latitude,

                Longitude =
                    (double)s.Address.Location!.Longitude
            })
            .ToListAsync();

        // Tính khoảng cách thực tế trước khi phân trang
        var nearby = candidates
            .Select(s => new NearbyShopResponseDto
            {
                ShopId = s.ShopId,
                ShopName = s.ShopName,
                Description = s.Description,
                Phone = s.Phone,
                LogoUrl = s.LogoUrl,
                RatingAverage = s.RatingAverage,
                Address = s.Address,

                DistanceKm = CalculateDistanceKm(
                    lat, lon, s.Latitude, s.Longitude)
            })
            .Where(s => s.DistanceKm <= radiusKm)
            .OrderBy(s => s.DistanceKm)
            .ThenBy(s => s.ShopId)
            .ToList();

        int total = nearby.Count;

        var data = nearby
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new
        {
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling(
                total / (double)pageSize),

            radiusKm,
            serviceId,
            data
        });
    }

    // Haversine: khoảng cách đường chim bay
    private static double CalculateDistanceKm(
        double lat1,
        double lon1,
        double lat2,
        double lon2)
    {
        const double earthRadiusKm = 6371.0;

        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);

        double a =
            Math.Pow(Math.Sin(dLat / 2), 2) +
            Math.Cos(ToRadians(lat1)) *
            Math.Cos(ToRadians(lat2)) *
            Math.Pow(Math.Sin(dLon / 2), 2);

        a = Math.Clamp(a, 0.0, 1.0);

        return 2 * earthRadiusKm *
            Math.Atan2(
                Math.Sqrt(a),
                Math.Sqrt(1 - a));
    }

    private static double ToRadians(double value)
    {
        return value * Math.PI / 180.0;
    }
}
