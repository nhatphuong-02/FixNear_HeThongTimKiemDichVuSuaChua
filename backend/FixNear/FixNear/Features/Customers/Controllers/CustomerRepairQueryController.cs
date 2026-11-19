using System.Security.Claims;
using FixNear.Domain.Enums;
using FixNear.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Customers.Controllers;

[ApiController]
[Authorize(Roles = "1")]
[Route("api/customers/me/repair-requests")]
public sealed class CustomerRepairQueryController : ControllerBase
{
    private readonly FixNearDbContext _db;

    public CustomerRepairQueryController(FixNearDbContext db)
    {
        _db = db;
    }

    private int? CurrentUserId()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : null;
    }

    // Danh sách yêu cầu của chính Customer, lọc và phân trang tại Backend.
    [HttpGet]
    public async Task<IActionResult> GetMyRepairRequests(
        [FromQuery] string? keyword,
        [FromQuery] RepairRequestStatus? status,
        [FromQuery] int? shopId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (page < 1 || pageSize is < 1 or > 50 ||
            (long)(page - 1) * pageSize > int.MaxValue ||
            (shopId.HasValue && shopId.Value <= 0))
            return BadRequest(new { message = "Tham số phân trang hoặc ShopId không hợp lệ." });

        if (from.HasValue && to.HasValue && from > to)
            return BadRequest(new { message = "Ngày bắt đầu phải trước ngày kết thúc." });

        if (status.HasValue && !Enum.IsDefined(status.Value))
            return BadRequest(new { message = "Trạng thái yêu cầu không hợp lệ." });

        keyword = keyword?.Trim();
        if (keyword?.Length > 100)
            return BadRequest(new { message = "Từ khóa không được vượt quá 100 ký tự." });

        var query = _db.RepairRequest
            .AsNoTracking()
            .Where(r => r.Customer.UserId == userId.Value);

        if (status.HasValue)
            query = query.Where(r => r.Status == (int)status.Value);

        if (shopId.HasValue)
            query = query.Where(r => r.ShopId == shopId.Value);

        // from/to lọc theo thời điểm tạo yêu cầu (UTC), không phải ngày hẹn sửa.
        if (from.HasValue)
            query = query.Where(r => r.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(r => r.CreatedAt <= to.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = "%" + keyword
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("_", "[_]") + "%";

            query = query.Where(r =>
                EF.Functions.Like(r.Shop.ShopName, pattern) ||
                (r.Description != null &&
                 EF.Functions.Like(r.Description, pattern)) ||
                r.RepairRequestService.Any(rs =>
                    EF.Functions.Like(rs.Service.ServiceName, pattern)));
        }

        var total = await query.CountAsync();

        var data = await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.RepairRequestId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.RepairRequestId,
                r.ShopId,
                r.Shop.ShopName,
                r.Shop.LogoUrl,
                r.Description,
                r.RepairMethod,
                r.Status,
                r.Priority,
                r.PreferredDate,
                r.PreferredTimeFrom,
                r.PreferredTimeTo,
                r.CreatedAt,
                r.UpdatedAt,
                r.CompletedAt,
                r.CancelledAt,
                Services = r.RepairRequestService
                    .OrderBy(rs => rs.ServiceId)
                    .Select(rs => new
                    {
                        rs.ServiceId,
                        rs.Service.ServiceName
                    }).ToList()
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

    // Chi tiết một yêu cầu, chỉ khi thuộc Customer đang đăng nhập.
    [HttpGet("{repairRequestId:int}")]
    public async Task<IActionResult> GetMyRepairRequest(int repairRequestId)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (repairRequestId <= 0)
            return BadRequest(new { message = "RepairRequestId không hợp lệ." });

        var data = await _db.RepairRequest
            .AsNoTracking()
            .Where(r => r.RepairRequestId == repairRequestId &&
                        r.Customer.UserId == userId.Value)
            .Select(r => new
            {
                r.RepairRequestId,
                r.ShopId,
                r.Shop.ShopName,
                r.Shop.LogoUrl,
                r.Shop.Phone,
                r.AddressId,
                Address = r.Address == null ? null : new
                {
                    r.Address.AddressLine,
                    WardName = r.Address.Ward.WardName,
                    ProvinceName = r.Address.Province.ProvinceName
                },
                r.RepairMethod,
                r.Description,
                r.PreferredDate,
                r.PreferredTimeFrom,
                r.PreferredTimeTo,
                r.Status,
                r.Priority,
                r.CreatedAt,
                r.UpdatedAt,
                r.CompletedAt,
                r.CancelledAt,
                Services = r.RepairRequestService
                    .OrderBy(rs => rs.ServiceId)
                    .Select(rs => new
                    {
                        rs.ServiceId,
                        rs.Service.ServiceName,
                        rs.Description,
                        rs.Note
                    }).ToList(),
                Images = r.RepairRequestImage
                    .OrderBy(i => i.DisplayOrder)
                    .ThenBy(i => i.RepairRequestImageId)
                    .Select(i => new
                    {
                        i.RepairRequestImageId,
                        i.ImageUrl,
                        i.Caption,
                        i.DisplayOrder
                    }).ToList(),
                Schedules = r.RepairSchedule
                    .Where(s => s.IsCurrent)
                    .OrderBy(s => s.StartTime)
                    .Select(s => new
                    {
                        s.RepairScheduleId,
                        s.StartTime,
                        s.EndTime,
                        s.Status,
                        s.TechnicianId,
                        TechnicianName = s.Technician.User.FullName
                    }).ToList(),
                StatusHistory = r.RepairRequestStatusHistory
                    .OrderByDescending(h => h.CreatedAt)
                    .ThenByDescending(h => h.HistoryId)
                    .Select(h => new
                    {
                        h.HistoryId,
                        h.OldStatus,
                        h.NewStatus,
                        h.Reason,
                        h.CreatedAt
                    }).ToList()
            })
            .FirstOrDefaultAsync();

        if (data == null)
            return NotFound(new { message = "Không tìm thấy yêu cầu sửa chữa." });

        return Ok(data);
    }
}
