using System.Security.Claims;
using FixNear.Domain.Enums;
using FixNear.Enums;
using FixNear.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Shops.Controllers;

[ApiController]
[Authorize(Roles = "2")]
[Route("api/shops/{shopId:int}/repair-requests/search")]
public sealed class ShopRepairSearchController : ControllerBase
{
    private readonly FixNearDbContext _db;

    public ShopRepairSearchController(FixNearDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        int shopId,
        [FromQuery] string? keyword,
        [FromQuery] RepairRequestStatus? status,
        [FromQuery] int? technicianId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var claim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(claim, out var userId))
            return Unauthorized();

        if (shopId <= 0 ||
            page < 1 ||
            pageSize < 1 ||
            pageSize > 50 ||
            (technicianId.HasValue &&
             technicianId.Value <= 0))
            return BadRequest(new
            {
                message = "Tham số không hợp lệ."
            });

        if (from.HasValue && to.HasValue &&
            from.Value > to.Value)
            return BadRequest(new
            {
                message = "Khoảng thời gian không hợp lệ."
            });

        if (keyword?.Length > 100)
            return BadRequest(new
            {
                message = "Từ khóa tối đa 100 ký tự."
            });

        if (status.HasValue &&
            !Enum.IsDefined(status.Value))
            return BadRequest(new
            {
                message = "Trạng thái sửa chữa không hợp lệ."
            });

        var hasPermission = await _db.ShopMember
            .AsNoTracking()
            .AnyAsync(sm =>
                sm.ShopId == shopId &&
                sm.Manager.UserId == userId &&
                sm.Status == ShopMemberStatus.Active &&
                sm.LeftAt == null &&
                (sm.ShopRole == ShopRole.Owner ||
                 sm.ShopRole == ShopRole.Manager));

        if (!hasPermission)
            return Forbid();

        var query = _db.RepairRequest
            .AsNoTracking()
            .Where(r => r.ShopId == shopId);

        if (status.HasValue)
            query = query.Where(r =>
                r.Status == (int)status.Value);

        if (technicianId.HasValue)
            query = query.Where(r =>
                r.RepairAssignment.Any(a =>
                    a.TechnicianId == technicianId.Value &&
                    a.AssignmentStatus ==
                        (int)RepairAssignmentStatus.Accepted));

        if (from.HasValue)
            query = query.Where(r =>
                r.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(r =>
                r.CreatedAt <= to.Value);

        keyword = keyword?.Trim();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = "%" +
                keyword.Replace("[", "[[]")
                       .Replace("%", "[%]")
                       .Replace("_", "[_]") +
                "%";

            query = query.Where(r =>
                EF.Functions.Like(
                    r.Customer.User.FullName,
                    pattern) ||
                (r.Description != null &&
                 EF.Functions.Like(
                    r.Description,
                    pattern)) ||
                r.RepairRequestService.Any(s =>
                    EF.Functions.Like(
                        s.Service.ServiceName,
                        pattern)));
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
                r.CustomerId,
                CustomerName = r.Customer.User.FullName,
                r.ShopId,
                r.Description,
                r.Status,
                r.Priority,
                r.RepairMethod,
                r.PreferredDate,
                r.PreferredTimeFrom,
                r.PreferredTimeTo,
                r.CreatedAt,

                Services = r.RepairRequestService
                    .Select(s => new
                    {
                        s.ServiceId,
                        s.Service.ServiceName
                    })
                    .ToList(),

                Technicians = r.RepairAssignment
                    .Where(a =>
                        a.AssignmentStatus ==
                        (int)RepairAssignmentStatus.Accepted)
                    .Select(a => new
                    {
                        a.TechnicianId,
                        TechnicianName =
                            a.Technician.User.FullName
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(new
        {
            shopId,
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling(
                total / (double)pageSize),
            data
        });
    }
}
