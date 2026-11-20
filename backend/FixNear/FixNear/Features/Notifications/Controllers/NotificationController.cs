
using System.Security.Claims;
using FixNear.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Notifications.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications/me")]
public sealed class NotificationController : ControllerBase
{
    private readonly FixNearDbContext _db;

    public NotificationController(FixNearDbContext db)
    {
        _db = db;
    }

    // Lấy UserId từ JWT của tài khoản đăng nhập
    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(claim, out var userId)
            ? userId
            : null;
    }

    // Xem danh sách thông báo, hỗ trợ lọc và phân trang
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isRead = null)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        if (page < 1 || pageSize < 1 || pageSize > 50)
            return BadRequest(new
            {
                message = "Phân trang không hợp lệ."
            });

        var query = _db.UserNotification
            .AsNoTracking()
            .Where(n => n.UserId == userId.Value);

        if (isRead.HasValue)
        {
            query = query.Where(n =>
                n.IsRead == isRead.Value);
        }

        var total = await query.CountAsync();

        var data = await query
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.UserNotificationId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new
            {
                n.UserNotificationId,
                n.NotificationId,

                n.Notification.Title,
                n.Notification.Body,

                n.Notification.RefType,
                n.Notification.RefId,

                TemplateCode = n.Notification.Template != null
                    ? n.Notification.Template.TemplateCode
                    : null,

                n.IsRead,
                n.ReadAt,
                n.CreatedAt
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

    // Số thông báo chưa đọc của tài khoản
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var count = await _db.UserNotification
            .AsNoTracking()
            .CountAsync(n =>
                n.UserId == userId.Value &&
                !n.IsRead);

        return Ok(new
        {
            unreadCount = count
        });
    }

    // Đánh dấu một thông báo đã đọc
    [HttpPatch("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var notification = await _db.UserNotification
            .FirstOrDefaultAsync(n =>
                n.UserNotificationId == id &&
                n.UserId == userId.Value);

        if (notification == null)
            return NotFound(new
            {
                message = "Không tìm thấy thông báo."
            });

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }

        return Ok(new
        {
            message = "Đã đánh dấu thông báo là đã đọc.",
            userNotificationId = id
        });
    }

    // Đánh dấu toàn bộ thông báo đã đọc
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var now = DateTime.UtcNow;

        var updatedCount = await _db.UserNotification
            .Where(n =>
                n.UserId == userId.Value &&
                !n.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now));

        return Ok(new
        {
            message = "Đã đọc tất cả thông báo.",
            updatedCount
        });
    }
}
