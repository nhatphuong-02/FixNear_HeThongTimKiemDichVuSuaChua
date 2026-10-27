
using FixNear.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Controllers.Manager;

[ApiController]
[Route("api/manager")]
public class ManagerShopController : ControllerBase
{
    private readonly FixNearDbContext _db;

    public ManagerShopController(FixNearDbContext db)
    {
        _db = db;
    }

    // T01: Xem thông tin cửa hàng
    // managerId chỉ dùng để test cục bộ.
    [HttpGet("shop")]
    public async Task<IActionResult> GetShop(
        [FromQuery] int managerId)
    {
        if (managerId <= 0)
            return BadRequest("ManagerId không hợp lệ");

        var shop = await _db.Shop
            .AsNoTracking()
            .Where(s => s.ManagerId == managerId)
            .Select(s => new
            {
                s.ShopId,
                s.ManagerId,
                s.ShopName,
                s.Description,
                s.Phone,
                s.Email,
                s.LogoUrl,
                s.AddressId,
                s.Status,
                s.IsVerified
            })
            .FirstOrDefaultAsync();

        if (shop == null)
            return NotFound("Không tìm thấy cửa hàng");

        return Ok(shop);
    }

    // DTO nhận dữ liệu cập nhật cửa hàng
    public class UpdateShopDto
    {
        public string ShopName { get; set; } = "";
        public string? Description { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    // T02: Cập nhật thông tin cửa hàng
    [HttpPut("shop")]
    public async Task<IActionResult> UpdateShop(
        [FromQuery] int managerId,
        [FromBody] UpdateShopDto dto)
    {
        if (managerId <= 0)
            return BadRequest("ManagerId không hợp lệ");

        if (string.IsNullOrWhiteSpace(dto.ShopName))
            return BadRequest("Tên cửa hàng không được trống");

        var shop = await _db.Shop
            .FirstOrDefaultAsync(
                s => s.ManagerId == managerId);

        if (shop == null)
            return NotFound("Không tìm thấy cửa hàng");

        shop.ShopName = dto.ShopName.Trim();
        shop.Description = dto.Description;
        shop.Phone = dto.Phone;
        shop.Email = dto.Email;
        shop.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Cập nhật cửa hàng thành công",
            shop.ShopId,
            shop.ShopName
        });
    }

    // T04: Xem danh sách dịch vụ của cửa hàng
   
    // T07: Xem yêu cầu sửa chữa
    [HttpGet("repair-requests")]
    public async Task<IActionResult> GetRequests(
        [FromQuery] int managerId)
    {
        var shopId = await _db.Shop
            .Where(s => s.ManagerId == managerId)
            .Select(s => (int?)s.ShopId)
            .FirstOrDefaultAsync();

        if (shopId == null)
            return NotFound("Không tìm thấy cửa hàng");

        var requests = await _db.RepairRequest
            .AsNoTracking()
            .Where(r => r.ShopId == shopId.Value)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.RepairRequestId,
                r.CustomerId,
                r.Description,
                r.Status,
                r.Priority,
                r.PreferredDate,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(requests);
    }
}
