using System.Security.Claims;
using FixNear.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Messaging.Hubs;

[Authorize(Roles = "1,2,3")]
public sealed class ChatHub : Hub
{
    private readonly FixNearDbContext _db;

    public ChatHub(FixNearDbContext db)
    {
        _db = db;
    }

    public static string GroupName(int conversationId)
        => $"conversation:{conversationId}";

    // Mobile gọi khi mở một cuộc trò chuyện
    public async Task JoinConversation(int conversationId)
    {
        var claim = Context.User?
            .FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(claim, out var userId))
            throw new HubException("Chưa xác thực.");

        var allowed = await _db.Conversation
            .AsNoTracking()
            .AnyAsync(c =>
                c.ConversationId == conversationId &&
                !c.IsDeleted &&
                c.Status == 1 &&
                c.RepairRequestId != null &&
                c.ConversationMember.Any(m =>
                    m.UserId == userId &&
                    m.LeftAt == null) &&
                (
                    c.RepairRequest!.Customer.UserId == userId ||
                    c.RepairRequest.Shop.ShopMembers.Any(sm =>
                        sm.Manager.UserId == userId &&
                        sm.Status == FixNear.Enums.ShopMemberStatus.Active &&
                        sm.LeftAt == null &&
                        (sm.ShopRole == FixNear.Enums.ShopRole.Owner ||
                         sm.ShopRole == FixNear.Enums.ShopRole.Manager)) ||
                    c.RepairRequest.RepairAssignment.Any(a =>
                        a.Technician.UserId == userId &&
                        a.AssignmentStatus == 2)
                ));

        if (!allowed)
            throw new HubException("Không có quyền truy cập hội thoại.");

        // Không thêm vào group: REST phát theo Clients.Users(...)
        // và xác thực lại người nhận tại thời điểm gửi.
        // Join chỉ có nhiệm vụ kiểm tra quyền khi mở hội thoại.
    }

    // Mobile gọi khi đóng cuộc trò chuyện
    public Task LeaveConversation(int conversationId)
    {
        return Task.CompletedTask;
    }
}
