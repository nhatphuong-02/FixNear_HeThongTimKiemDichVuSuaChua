
using System.Security.Claims;
using FixNear.Domain.Enums;
using FixNear.Features.Repairs.DTOs;
using FixNear.Infrastructure.Persistence;
using FixNear.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Repairs.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class UpcomingSchedulesController : ControllerBase
{
    private readonly FixNearDbContext _db;

    private const int MaxUpcoming = 5;

    public UpcomingSchedulesController(FixNearDbContext db)
    {
        _db = db;
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(claim, out var userId)
            ? userId
            : null;
    }

    // Truy vấn lịch còn hiệu lực và chưa kết thúc
    private IQueryable<FixNear.Domain.Models.Repairs.RepairSchedule>
        GetActiveSchedules()
    {
        var now = DateTime.UtcNow;

        return _db.RepairSchedule
            .AsNoTracking()
            .Where(s =>
                s.IsCurrent &&
                s.EndTime > now &&

                (s.Status ==
                    (int)RepairScheduleStatus.Scheduled ||
                 s.Status ==
                    (int)RepairScheduleStatus.Rescheduled) &&

                s.RepairRequest.Status !=
                    (int)RepairRequestStatus.Rejected &&
                s.RepairRequest.Status !=
                    (int)RepairRequestStatus.Cancelled &&
                s.RepairRequest.Status !=
                    (int)RepairRequestStatus.Completed &&
                s.RepairRequest.Status !=
                    (int)RepairRequestStatus.Paid);
    }

    // Cấu trúc dữ liệu trả về dùng chung
    private static IQueryable<UpcomingScheduleDto> ProjectSchedules(
        IQueryable<FixNear.Domain.Models.Repairs.RepairSchedule> query)
    {
        return query
            .OrderBy(s => s.StartTime)
            .ThenBy(s => s.RepairScheduleId)
            .Take(MaxUpcoming)
            .Select(s => new UpcomingScheduleDto
            {
                RepairScheduleId = s.RepairScheduleId,
                RepairRequestId = s.RepairRequestId,

                ShopId = s.RepairRequest.ShopId,
                ShopName =
                    s.RepairRequest.Shop.ShopName,

                CustomerId =
                    s.RepairRequest.CustomerId,
                CustomerName =
                    s.RepairRequest.Customer.User.FullName,

                TechnicianId = s.TechnicianId,
                TechnicianName =
                    s.Technician.User.FullName,

                Description =
                    s.RepairRequest.Description,

                RepairMethod =
                    s.RepairRequest.RepairMethod,

                RepairStatus =
                    s.RepairRequest.Status,

                ScheduleStatus = s.Status,

                StartTime = s.StartTime,
                EndTime = s.EndTime,

                ServiceNames = s.RepairRequest
                    .RepairRequestService
                    .OrderBy(rs => rs.ServiceId)
                    .Select(rs => rs.Service.ServiceName)
                    .ToList()
            });
    }

    // Customer: lịch sửa chữa sắp tới của mình
    [Authorize(Roles = "1")]
    [HttpGet("customers/me/upcoming-schedules")]
    public async Task<IActionResult> GetCustomerSchedules()
    {
        var userId = GetCurrentUserId();

        if (!userId.HasValue)
            return Unauthorized();

        var query = GetActiveSchedules()
            .Where(s =>
                s.RepairRequest.Customer.UserId ==
                userId.Value);

        var data = await ProjectSchedules(query)
            .ToListAsync();

        return Ok(new
        {
            total = data.Count,
            data
        });
    }

    // Technician: lịch được phân công cho mình
    [Authorize(Roles = "3")]
    [HttpGet("technician/me/upcoming-schedules")]
    public async Task<IActionResult> GetTechnicianSchedules()
    {
        var userId = GetCurrentUserId();

        if (!userId.HasValue)
            return Unauthorized();

        var query = GetActiveSchedules()
            .Where(s =>
                s.Technician.UserId == userId.Value);

        var data = await ProjectSchedules(query)
            .ToListAsync();

        return Ok(new
        {
            total = data.Count,
            data
        });
    }

    // Shop: lịch sắp tới của cửa hàng
    [Authorize(Roles = "2")]
    [HttpGet("shops/{shopId:int}/upcoming-schedules")]
    public async Task<IActionResult> GetShopSchedules(int shopId)
    {
        if (shopId <= 0)
        {
            return BadRequest(new
            {
                message = "ShopId không hợp lệ."
            });
        }

        var userId = GetCurrentUserId();

        if (!userId.HasValue)
            return Unauthorized();

        // Chỉ Owner / Manager được xem lịch Shop
        var hasAccess = await _db.ShopMember
            .AsNoTracking()
            .AnyAsync(sm =>
                sm.ShopId == shopId &&
                sm.Manager.UserId == userId.Value &&
                sm.Status == ShopMemberStatus.Active &&
                sm.LeftAt == null &&
                (sm.ShopRole == ShopRole.Owner ||
                 sm.ShopRole == ShopRole.Manager));

        if (!hasAccess)
            return Forbid();

        var query = GetActiveSchedules()
            .Where(s =>
                s.RepairRequest.ShopId == shopId);

        var data = await ProjectSchedules(query)
            .ToListAsync();

        return Ok(new
        {
            shopId,
            total = data.Count,
            data
        });
    }
}
