using FixNear.Domain.Models.Messaging;
using FixNear.Features.Messaging.DTOs;
using FixNear.Features.Messaging.Hubs;
using FixNear.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace FixNear.Features.Messaging.Controllers;

[ApiController]
[Authorize(Roles = "1,2,3")]
[Route("api/conversations")]
public sealed class MessagingController : ControllerBase
{
    private readonly FixNearDbContext _db;
    private readonly IHubContext<ChatHub> _hub;

    public MessagingController(
    FixNearDbContext db,
    IHubContext<ChatHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    private int? CurrentUserId()
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var id) ? id : null;
    }

    // Kiểm tra quan hệ nghiệp vụ với RepairRequest
    private async Task<bool> HasRepairAccessAsync(
        int repairRequestId, int userId)
    {
        return await _db.RepairRequest
            .AsNoTracking()
            .AnyAsync(r =>
                r.RepairRequestId == repairRequestId &&
                (
                    r.Customer.UserId == userId ||

                    r.Shop.ShopMembers.Any(sm =>
                        sm.Manager.UserId == userId &&
                        sm.Status == FixNear.Enums.ShopMemberStatus.Active &&
                        sm.LeftAt == null &&
                        (sm.ShopRole == FixNear.Enums.ShopRole.Owner ||
                         sm.ShopRole == FixNear.Enums.ShopRole.Manager)) ||

                    r.RepairAssignment.Any(a =>
                        a.Technician.UserId == userId &&
                        a.AssignmentStatus == 2)
                ));
    }

    // Thành viên hội thoại phải còn quyền với RepairRequest
    private async Task<bool> CanAccessAsync(
        int conversationId, int userId)
    {
        var conversation = await _db.Conversation
            .AsNoTracking()
            .Where(c =>
                c.ConversationId == conversationId &&
                !c.IsDeleted &&
                c.Status == 1 &&
                c.ConversationMember.Any(m =>
                    m.UserId == userId &&
                    m.LeftAt == null))
            .Select(c => new
            {
                c.RepairRequestId
            })
            .FirstOrDefaultAsync();

        return conversation?.RepairRequestId is int repairId &&
            await HasRepairAccessAsync(repairId, userId);
    }

    // Đồng bộ thành viên hiện hành. Không xóa vật lý thành viên vì
    // Message dùng khóa ngoại ghép đến ConversationMember.
    private async Task SyncMembersAsync(int conversationId, int repairId)
    {
        var repair = await _db.RepairRequest.AsNoTracking()
            .Where(r => r.RepairRequestId == repairId)
            .Select(r => new { CustomerUserId = r.Customer.UserId, r.ShopId })
            .SingleAsync();

        var managerIds = await _db.ShopMember.AsNoTracking()
            .Where(sm => sm.ShopId == repair.ShopId &&
                sm.Status == FixNear.Enums.ShopMemberStatus.Active &&
                sm.LeftAt == null &&
                (sm.ShopRole == FixNear.Enums.ShopRole.Owner ||
                 sm.ShopRole == FixNear.Enums.ShopRole.Manager))
            .Select(sm => sm.Manager.UserId).ToListAsync();

        var technicianIds = await _db.RepairAssignment.AsNoTracking()
            .Where(a => a.RepairRequestId == repairId && a.AssignmentStatus == 2)
            .Select(a => a.Technician.UserId).ToListAsync();

        var validIds = managerIds.Concat(technicianIds)
            .Append(repair.CustomerUserId).ToHashSet();

        var members = await _db.ConversationMember
            .Where(m => m.ConversationId == conversationId).ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var m in members)
        {
            if (validIds.Contains(m.UserId))
            {
                if (m.LeftAt.HasValue)
                {
                    m.LeftAt = null;
                    m.JoinedAt = now;
                    m.LastReadAt = now;
                }
            }
            else if (!m.LeftAt.HasValue)
            {
                m.LeftAt = now;
            }
        }

        var existing = members.Select(m => m.UserId).ToHashSet();
        foreach (var id in validIds.Except(existing))
        {
            _db.ConversationMember.Add(new ConversationMember
            {
                ConversationId = conversationId,
                UserId = id,
                JoinedAt = now,
                CreatedAt = now,
                LastReadAt = now
            });
        }

        await _db.SaveChangesAsync();
    }

    // Tạo hoặc lấy hội thoại cho một yêu cầu sửa chữa
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateConversationRequestDto request)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (!await HasRepairAccessAsync(
                request.RepairRequestId, userId.Value))
            return Forbid();

        // Serializable giúp tránh tạo hội thoại trùng khi
        // nhiều request đến đồng thời.
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var existing = await _db.Conversation
            .Where(c =>
                c.RepairRequestId == request.RepairRequestId &&
                !c.IsDeleted)
            .OrderBy(c => c.ConversationId)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            await SyncMembersAsync(existing.ConversationId, request.RepairRequestId);
            await transaction.CommitAsync();

            if (!await CanAccessAsync(
                existing.ConversationId, userId.Value))
                return Forbid();

            return Ok(new
            {
                existing.ConversationId,
                existing.RepairRequestId
            });
        }

        var repair = await _db.RepairRequest
            .AsNoTracking()
            .Where(r =>
                r.RepairRequestId == request.RepairRequestId)
            .Select(r => new
            {
                r.Customer.UserId,
                r.ShopId
            })
            .FirstOrDefaultAsync();

        if (repair == null)
            return NotFound();

        var managerIds = await _db.ShopMember
            .AsNoTracking()
            .Where(sm =>
                sm.ShopId == repair.ShopId &&
                sm.Status == FixNear.Enums.ShopMemberStatus.Active &&
                sm.LeftAt == null &&
                (sm.ShopRole == FixNear.Enums.ShopRole.Owner ||
                 sm.ShopRole == FixNear.Enums.ShopRole.Manager))
            .Select(sm => sm.Manager.UserId)
            .ToListAsync();

        var technicianIds = await _db.RepairAssignment
            .AsNoTracking()
            .Where(a =>
                a.RepairRequestId == request.RepairRequestId &&
                a.AssignmentStatus == 2)
            .Select(a => a.Technician.UserId)
            .ToListAsync();

        var memberIds = managerIds
            .Concat(technicianIds)
            .Append(repair.UserId)
            .Distinct()
            .ToList();

        var now = DateTime.UtcNow;

        var conversation = new Conversation
        {
            RepairRequestId = request.RepairRequestId,
            Status = 1,
            CreatedAt = now,
            IsDeleted = false,
            ConversationMember = memberIds
                .Select(id => new ConversationMember
                {
                    UserId = id,
                    JoinedAt = now,
                    CreatedAt = now,
                    LastReadAt = now
                })
                .ToList()
        };

        _db.Conversation.Add(conversation);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return StatusCode(201, new
        {
            conversation.ConversationId,
            conversation.RepairRequestId
        });
    }

    // Danh sách hội thoại của tài khoản
    [HttpGet]
    public async Task<IActionResult> GetConversations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (page < 1 || pageSize < 1 ||
            pageSize > 50 || (long)(page - 1) * pageSize > int.MaxValue)
            return BadRequest(new { message = "Phân trang không hợp lệ." });

        var query = _db.Conversation
            .AsNoTracking()
            .Where(c =>
                !c.IsDeleted &&
                c.Status == 1 &&
                c.RepairRequestId != null &&
                c.ConversationMember.Any(m =>
                    m.UserId == userId.Value &&
                    m.LeftAt == null) &&
                (
                    c.RepairRequest!.Customer.UserId == userId.Value ||
                    c.RepairRequest.Shop.ShopMembers.Any(sm =>
                        sm.Manager.UserId == userId.Value &&
                        sm.Status == FixNear.Enums.ShopMemberStatus.Active &&
                        sm.LeftAt == null &&
                        (sm.ShopRole == FixNear.Enums.ShopRole.Owner ||
                         sm.ShopRole == FixNear.Enums.ShopRole.Manager)) ||
                    c.RepairRequest.RepairAssignment.Any(a =>
                        a.Technician.UserId == userId.Value &&
                        a.AssignmentStatus == 2)
                ));

        var total = await query.CountAsync();

        var data = await query
            .OrderByDescending(c =>
                c.Message.Select(m => (DateTime?)m.CreatedAt)
                    .Max() ?? c.CreatedAt)
            .ThenByDescending(c => c.ConversationId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.ConversationId,
                c.RepairRequestId,
                c.CreatedAt,
                LastMessage = c.Message
                    .Where(m => !m.IsDeleted)
                    .OrderByDescending(m => m.MessageId)
                    .Select(m => new
                    {
                        m.MessageId,
                        m.SenderUserId,
                        m.Content,
                        m.CreatedAt
                    })
                    .FirstOrDefault()
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

    // Thông tin một hội thoại
    [HttpGet("{conversationId:int}")]
    public async Task<IActionResult> GetConversation(
        int conversationId)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (!await CanAccessAsync(conversationId, userId.Value))
            return NotFound(new { message = "Không tìm thấy hội thoại." });

        var result = await _db.Conversation
            .AsNoTracking()
            .Where(c => c.ConversationId == conversationId)
            .Select(c => new
            {
                c.ConversationId,
                c.RepairRequestId,
                c.CreatedAt,
                Members = c.ConversationMember
                    .Where(m => m.LeftAt == null)
                    .Select(m => new
                    {
                        m.UserId,
                        m.User.FullName,
                        m.User.AvatarUrl
                    })
                    .ToList()
            })
            .FirstAsync();

        return Ok(result);
    }

    // Xem lịch sử tin nhắn, mới nhất trước
    [HttpGet("{conversationId:int}/messages")]
    public async Task<IActionResult> GetMessages(
        int conversationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (!await CanAccessAsync(conversationId, userId.Value))
            return NotFound(new { message = "Không tìm thấy hội thoại." });

        if (page < 1 || pageSize < 1 ||
            pageSize > 50 || (long)(page - 1) * pageSize > int.MaxValue)
            return BadRequest(new { message = "Phân trang không hợp lệ." });

        var query = _db.Message
            .AsNoTracking()
            .Where(m =>
                m.ConversationId == conversationId &&
                !m.IsDeleted);

        var total = await query.CountAsync();

        var data = await query
            .OrderByDescending(m => m.MessageId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                m.MessageId,
                m.ConversationId,
                m.SenderUserId,
                SenderName = m.SenderUser.FullName,
                m.Content,
                m.CreatedAt,
                Attachments = m.MessageAttachment.Select(a => new
                {
                    a.MessageAttachmentId,
                    a.FileType
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

    // Đếm tin chưa đọc cho toàn bộ cuộc trò chuyện được phép xem
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var total = await _db.ConversationMember.AsNoTracking()
            .Where(m => m.UserId == userId.Value && m.LeftAt == null &&
                !m.Conversation.IsDeleted && m.Conversation.Status == 1 &&
                m.Conversation.RepairRequestId != null &&
                (m.Conversation.RepairRequest!.Customer.UserId == userId.Value ||
                 m.Conversation.RepairRequest.Shop.ShopMembers.Any(sm =>
                    sm.Manager.UserId == userId.Value &&
                    sm.Status == FixNear.Enums.ShopMemberStatus.Active &&
                    sm.LeftAt == null &&
                    (sm.ShopRole == FixNear.Enums.ShopRole.Owner ||
                     sm.ShopRole == FixNear.Enums.ShopRole.Manager)) ||
                 m.Conversation.RepairRequest.RepairAssignment.Any(a =>
                    a.Technician.UserId == userId.Value && a.AssignmentStatus == 2)))
            .SumAsync(m => m.Conversation.Message.Count(x =>
                !x.IsDeleted && x.SenderUserId != userId.Value &&
                x.CreatedAt > (m.LastReadAt ?? m.JoinedAt)));
        return Ok(new { unreadCount = total });
    }

    // Đánh dấu đã đọc đến tin nhắn mới nhất đã lưu
    [HttpPatch("{conversationId:int}/read")]
    public async Task<IActionResult> MarkRead(int conversationId)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return Unauthorized();
        if (!await CanAccessAsync(conversationId, userId.Value))
            return NotFound(new { message = "Không tìm thấy hội thoại." });

        var membership = await _db.ConversationMember.SingleAsync(m =>
            m.ConversationId == conversationId &&
            m.UserId == userId.Value && m.LeftAt == null);

        var latestTime = await _db.Message.AsNoTracking()
            .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
            .MaxAsync(m => (DateTime?)m.CreatedAt);

        var threshold = latestTime ?? membership.JoinedAt;
        if (!membership.LastReadAt.HasValue || membership.LastReadAt < threshold)
        {
            membership.LastReadAt = threshold;
            await _db.SaveChangesAsync();
        }
        return Ok(new { conversationId, membership.LastReadAt });
    }

    // Gửi tin nhắn bằng tài khoản JWT hiện tại
    [HttpPost("{conversationId:int}/messages")]
    public async Task<IActionResult> SendMessage(
        int conversationId,
        [FromBody] SendMessageRequestDto request)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (!await CanAccessAsync(conversationId, userId.Value))
            return NotFound(new { message = "Không tìm thấy hội thoại." });

        var content = request.Content?.Trim();

        if (string.IsNullOrWhiteSpace(content))
            return BadRequest(new { message = "Nội dung không được rỗng." });

        var message = new Message
        {
            ConversationId = conversationId,
            SenderUserId = userId.Value,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _db.Message.Add(message);
        await _db.SaveChangesAsync();

        // Phát tới tài khoản còn quyền HIỆN TẠI, không phát tới nhóm
        // conversation vì connection cũ có thể đã bị thu hồi quyền.
        var recipientIds = await _db.ConversationMember.AsNoTracking()
            .Where(m => m.ConversationId == conversationId && m.LeftAt == null &&
                m.UserId != userId.Value &&
                (m.Conversation.RepairRequest!.Customer.UserId == m.UserId ||
                 m.Conversation.RepairRequest.Shop.ShopMembers.Any(sm =>
                    sm.Manager.UserId == m.UserId &&
                    sm.Status == FixNear.Enums.ShopMemberStatus.Active &&
                    sm.LeftAt == null &&
                    (sm.ShopRole == FixNear.Enums.ShopRole.Owner ||
                     sm.ShopRole == FixNear.Enums.ShopRole.Manager)) ||
                 m.Conversation.RepairRequest.RepairAssignment.Any(a =>
                    a.Technician.UserId == m.UserId && a.AssignmentStatus == 2)))
            .Select(m => m.UserId.ToString()).ToListAsync();

        // Tin đã lưu DB, lỗi SignalR không được biến thành lỗi gửi tin REST.
        try
        {
            await _hub.Clients.Users(recipientIds).SendAsync(
                "ReceiveMessage", new
                {
                    message.MessageId,
                    message.ConversationId,
                    message.SenderUserId,
                    message.Content,
                    message.CreatedAt
                });
        }
        catch (Exception)
        {
            // Mobile đồng bộ lại tin khi kết nối lại.
            // Production nên có outbox + ILogger tại đây.
        }

        return StatusCode(201, new
        {
            message.MessageId,
            message.ConversationId,
            message.SenderUserId,
            message.Content,
            message.CreatedAt
        });
    }
}
