
using FixNear.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Controllers.Manager;

[ApiController]
[Route("api/manager/services")]
public class ManagerServiceController : ControllerBase
{
    private readonly FixNearDbContext _db;

    public ManagerServiceController(FixNearDbContext db)
    {
        _db = db;
    }

    // Danh mục dịch vụ chung
    [HttpGet("catalog")]
    public async Task<IActionResult> GetCatalog()
    {
        var data = await _db.Service
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => new {
                x.ServiceId,
                x.ServiceName,
                x.Description
            }).ToListAsync();

        return Ok(data);
    }

    // Danh sách dịch vụ của cửa hàng
    [HttpGet]
    public async Task<IActionResult> GetAll(int managerId)
    {
        var shop = await _db.Shop
            .FirstOrDefaultAsync(x => x.ManagerId == managerId);

        if (shop == null)
            return NotFound("Không tìm thấy cửa hàng");

        var data = await _db.ShopService
            .AsNoTracking()
            .Where(x => x.ShopId == shop.ShopId)
            .Select(x => new {
                x.ShopServiceId,
                x.ServiceId,
                x.Service.ServiceName,
                x.Description,
                x.IsActive,
                Price = x.ServicePrice == null
                    ? (decimal?)null
                    : x.ServicePrice.Price
            }).ToListAsync();

        return Ok(data);
    }

    public class AddServiceDto
    {
        public int ServiceId { get; set; }
        public string? Description { get; set; }
    }

    // Thêm dịch vụ vào cửa hàng
    [HttpPost]
    public async Task<IActionResult> Add(
        int managerId,
        [FromBody] AddServiceDto dto)
    {
        var shop = await _db.Shop
            .FirstOrDefaultAsync(x => x.ManagerId == managerId);

        if (shop == null)
            return NotFound("Không tìm thấy cửa hàng");

        bool exists = await _db.Service
            .AnyAsync(x => x.ServiceId == dto.ServiceId
                        && x.IsActive);

        if (!exists)
            return BadRequest("Dịch vụ không tồn tại");

        var old = await _db.ShopService
            .FirstOrDefaultAsync(x =>
                x.ShopId == shop.ShopId &&
                x.ServiceId == dto.ServiceId);

        if (old != null)
        {
            if (old.IsActive)
                return Conflict("Dịch vụ đã tồn tại");

            old.IsActive = true;
            old.Description = dto.Description;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Đã kích hoạt lại dịch vụ"
            });
        }

        var item = new ShopService
        {
            ShopId = shop.ShopId,
            ServiceId = dto.ServiceId,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _db.ShopService.Add(item);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Thêm dịch vụ thành công",
            item.ShopServiceId
        });
    }

    // Sửa mô tả dịch vụ
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id, int managerId,
        [FromBody] AddServiceDto dto)
    {
        var item = await _db.ShopService
            .FirstOrDefaultAsync(x =>
                x.ShopServiceId == id &&
                x.Shop.ManagerId == managerId);

        if (item == null)
            return NotFound("Không tìm thấy dịch vụ");

        if (item.ServiceId != dto.ServiceId)
            return BadRequest(
                "Không được đổi mã dịch vụ gốc");

        item.Description = dto.Description;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Cập nhật thành công"
        });
    }

    // Ngừng cung cấp, không xóa dữ liệu cũ
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Disable(
        int id, int managerId)
    {
        var item = await _db.ShopService
            .FirstOrDefaultAsync(x =>
                x.ShopServiceId == id &&
                x.Shop.ManagerId == managerId);

        if (item == null)
            return NotFound("Không tìm thấy dịch vụ");

        item.IsActive = false;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Đã ngừng cung cấp dịch vụ"
        });
    }
}
