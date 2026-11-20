
using System.Security.Claims;
using FixNear.Domain.Enums;
using FixNear.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Technicians.Controllers;

[ApiController]
[Authorize(Roles = "3")]
[Route("api/technician/assignments/search")]
public sealed class TechnicianAssignmentSearchController
    : ControllerBase
{
    private readonly FixNearDbContext _db;

    public TechnicianAssignmentSearchController(
        FixNearDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? keyword,
        [FromQuery] int? shopId,
        [FromQuery] RepairAssignmentStatus? assignmentStatus,
        [FromQuery] RepairRequestStatus? repairStatus,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var claim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(claim, out var userId))
            return Unauthorized();

        if (page < 1 || pageSize < 1 || pageSize > 50)
            return BadRequest(new
            {
                message = "Phân trang không hợp lệ."
            });

        if (keyword?.Length > 100)
            return BadRequest(new
            {
                message = "Từ khóa tối đa 100 ký tự."
            });

        if ((shopId.HasValue && shopId.Value <= 0) ||
            (from.HasValue && to.HasValue &&
             from.Value > to.Value))
            return BadRequest(new
            {
                message = "Bộ lọc không hợp lệ."
            });

        if (assignmentStatus.HasValue &&
            !Enum.IsDefined(assignmentStatus.Value))
            return BadRequest(new
            {
                message = "Trạng thái phân công không hợp lệ."
            });

        if (repairStatus.HasValue &&
            !Enum.IsDefined(repairStatus.Value))
            return BadRequest(new
            {
                message = "Trạng thái sửa chữa không hợp lệ."
            });

        var hasProfile = await _db.TechnicianProfile
            .AnyAsync(t => t.UserId == userId);

        if (!hasProfile)
            return NotFound(new
            {
                message = "Không tìm thấy hồ sơ kỹ thuật viên."
            });

        var query = _db.RepairAssignment
            .AsNoTracking()
            .Where(a => a.Technician.UserId == userId);

        if (shopId.HasValue)
            query = query.Where(a =>
                a.RepairRequest.ShopId == shopId.Value);

        if (assignmentStatus.HasValue)
            query = query.Where(a =>
                a.AssignmentStatus ==
                (int)assignmentStatus.Value);

        if (repairStatus.HasValue)
            query = query.Where(a =>
                a.RepairRequest.Status ==
                (int)repairStatus.Value);

        if (from.HasValue)
            query = query.Where(a =>
                a.AssignedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(a =>
                a.AssignedAt <= to.Value);

        keyword = keyword?.Trim();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = "%" +
                keyword.Replace("[", "[[]")
                       .Replace("%", "[%]")
                       .Replace("_", "[_]") +
                "%";

            query = query.Where(a =>
                EF.Functions.Like(
                    a.RepairRequest.Shop.ShopName,
                    pattern) ||
                EF.Functions.Like(
                    a.RepairRequest.Customer.User.FullName,
                    pattern) ||
                (a.RepairRequest.Description != null &&
                 EF.Functions.Like(
                    a.RepairRequest.Description,
                    pattern)));
        }

        var total = await query.CountAsync();

        var data = await query
            .OrderByDescending(a => a.AssignedAt)
            .ThenByDescending(a => a.RepairAssignmentId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.RepairAssignmentId,
                a.RepairRequestId,
                a.AssignmentStatus,
                a.AssignedAt,
                a.RespondedAt,

                a.RepairRequest.ShopId,
                a.RepairRequest.Shop.ShopName,

                CustomerName =
                    a.RepairRequest.Customer.User.FullName,

                a.RepairRequest.Description,
                RepairStatus = a.RepairRequest.Status,
                a.RepairRequest.RepairMethod,
                a.RepairRequest.PreferredDate,
                a.RepairRequest.PreferredTimeFrom,
                a.RepairRequest.PreferredTimeTo,

                Services = a.RepairRequest
                    .RepairRequestService
                    .Select(s => new
                    {
                        s.ServiceId,
                        s.Service.ServiceName
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling(
                total / (double)pageSize),
            data
        });
    }
}
