using System.Data;
using FixNear.Domain.Enums;
using FixNear.Domain.Models.Notifications;
using FixNear.Enums;
using FixNear.Features.Technicians.DTOs;
using FixNear.Features.Technicians.Interfaces;
using FixNear.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Technicians.Services;

public sealed class TechnicianAssignmentService
    : ITechnicianAssignmentService
{
    private readonly FixNearDbContext _db;

    public TechnicianAssignmentService(FixNearDbContext db)
    {
        _db = db;
    }

    // Danh sách công việc của kỹ thuật viên đăng nhập
    public async Task<List<TechnicianAssignmentDto>>
        GetMyAssignmentsAsync(int userId)
    {
        await EnsureTechnicianAsync(userId);

        return await AssignmentQuery(userId)
            .OrderByDescending(a => a.AssignedAt)
            .Select(a => new TechnicianAssignmentDto
            {
                RepairAssignmentId = a.RepairAssignmentId,
                RepairRequestId = a.RepairRequestId,
                ShopId = a.RepairRequest.ShopId,
                ShopName = a.RepairRequest.Shop.ShopName,
                CustomerId = a.RepairRequest.CustomerId,
                CustomerName =
                    a.RepairRequest.Customer.User.FullName,
                Description = a.RepairRequest.Description,
                RepairMethod = a.RepairRequest.RepairMethod,
                PreferredDate = a.RepairRequest.PreferredDate,
                PreferredTimeFrom =
                    a.RepairRequest.PreferredTimeFrom,
                PreferredTimeTo =
                    a.RepairRequest.PreferredTimeTo,
                RepairStatus = a.RepairRequest.Status,
                AssignmentStatus = a.AssignmentStatus,
                AssignedAt = a.AssignedAt,
                RespondedAt = a.RespondedAt,
                Note = a.Note,
                RejectionReason = a.RejectionReason,

                Services = a.RepairRequest.RepairRequestService
                    .Select(s => new AssignmentServiceDto
                    {
                        ServiceId = s.ServiceId,
                        ServiceName = s.Service.ServiceName
                    })
                    .ToList()
            })
            .ToListAsync();
    }

    // Chi tiết một công việc thuộc kỹ thuật viên
    public async Task<TechnicianAssignmentDto?>
        GetAssignmentDetailAsync(
            int userId,
            int assignmentId)
    {
        await EnsureTechnicianAsync(userId);

        return await AssignmentQuery(userId)
            .Where(a => a.RepairAssignmentId == assignmentId)
            .Select(a => new TechnicianAssignmentDto
            {
                RepairAssignmentId = a.RepairAssignmentId,
                RepairRequestId = a.RepairRequestId,
                ShopId = a.RepairRequest.ShopId,
                ShopName = a.RepairRequest.Shop.ShopName,
                CustomerId = a.RepairRequest.CustomerId,
                CustomerName =
                    a.RepairRequest.Customer.User.FullName,
                Description = a.RepairRequest.Description,
                RepairMethod = a.RepairRequest.RepairMethod,
                PreferredDate = a.RepairRequest.PreferredDate,
                PreferredTimeFrom =
                    a.RepairRequest.PreferredTimeFrom,
                PreferredTimeTo =
                    a.RepairRequest.PreferredTimeTo,
                RepairStatus = a.RepairRequest.Status,
                AssignmentStatus = a.AssignmentStatus,
                AssignedAt = a.AssignedAt,
                RespondedAt = a.RespondedAt,
                Note = a.Note,
                RejectionReason = a.RejectionReason,

                Services = a.RepairRequest.RepairRequestService
                    .Select(s => new AssignmentServiceDto
                    {
                        ServiceId = s.ServiceId,
                        ServiceName = s.Service.ServiceName
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    // Kỹ thuật viên chấp nhận công việc được phân công
    public async Task AcceptAssignmentAsync(
        int userId,
        int assignmentId)
    {
        var technician = await EnsureTechnicianAsync(userId);

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var assignment = await _db.RepairAssignment
                .Include(a => a.RepairRequest)
                    .ThenInclude(r => r.Shop)
                .Include(a => a.RepairRequest)
                    .ThenInclude(r => r.Customer)
                .FirstOrDefaultAsync(a =>
                    a.RepairAssignmentId == assignmentId &&
                    a.TechnicianId == technician.TechnicianId);

            if (assignment == null)
                throw new KeyNotFoundException(
                    "Không tìm thấy phân công của bạn.");

            if (assignment.AssignmentStatus !=
                (int)RepairAssignmentStatus.Offered)
                throw new InvalidOperationException(
                    "Phân công không còn chờ phản hồi.");

            var repair = assignment.RepairRequest;

            if (repair.Status !=
                (int)RepairRequestStatus.Accepted)
                throw new InvalidOperationException(
                    "Yêu cầu sửa chữa không ở trạng thái chờ nhận việc.");

            bool isShopTechnician = await _db.TechnicianShop
                .AnyAsync(ts =>
                    ts.TechnicianId == technician.TechnicianId &&
                    ts.ShopId == repair.ShopId &&
                    ts.Status == TechnicianShopStatus.Active &&
                    ts.LeftAt == null);

            if (!isShopTechnician)
                throw new UnauthorizedAccessException(
                    "Bạn không còn là kỹ thuật viên của cửa hàng.");

            bool otherAccepted = await _db.RepairAssignment
                .AnyAsync(a =>
                    a.RepairRequestId == repair.RepairRequestId &&
                    a.RepairAssignmentId != assignmentId &&
                    a.AssignmentStatus ==
                        (int)RepairAssignmentStatus.Accepted);

            if (otherAccepted)
                throw new InvalidOperationException(
                    "Yêu cầu đã có kỹ thuật viên khác nhận.");

            var now = DateTime.UtcNow;

            // Phản hồi phân công
            assignment.AssignmentStatus =
                (int)RepairAssignmentStatus.Accepted;
            assignment.RespondedAt = now;

            // Cập nhật yêu cầu sửa chữa
            repair.Status = (int)RepairRequestStatus.Assigned;
            repair.UpdatedAt = now;

            await SetHistoryUserAsync(userId);

            // SQL trigger tự ghi lịch sử Repair
            await _db.SaveChangesAsync();

            // Tự đồng bộ thành viên khi KTV chấp nhận phân công.
            // Chạy trong cùng transaction với việc nhận việc.
            var conversationIds = await _db.Conversation
                .Where(c => c.RepairRequestId == repair.RepairRequestId &&
                            !c.IsDeleted && c.Status == 1)
                .Select(c => c.ConversationId)
                .ToListAsync();

            foreach (var conversationId in conversationIds)
            {
                var member = await _db.ConversationMember
                    .FirstOrDefaultAsync(m =>
                        m.ConversationId == conversationId &&
                        m.UserId == userId);

                if (member == null)
                {
                    _db.ConversationMember.Add(
                        new FixNear.Domain.Models.Messaging.ConversationMember
                        {
                            ConversationId = conversationId,
                            UserId = userId,
                            JoinedAt = now,
                            CreatedAt = now,
                            LastReadAt = now
                        });
                }
                else if (member.LeftAt.HasValue)
                {
                    member.LeftAt = null;
                    member.JoinedAt = now;
                    member.LastReadAt = now;
                }
            }

            await _db.SaveChangesAsync();

            var values = new Dictionary<string, string>
            {
                ["TechnicianName"] = technician.User.FullName,
                ["RequestId"] =
                    repair.RepairRequestId.ToString()
            };

            // Thông báo về Shop
            var managerUserIds = await GetShopManagersAsync(
                repair.ShopId);

            await AddNotificationAsync(
                "ASSIGNMENT_ACCEPTED",
                managerUserIds,
                repair.RepairRequestId,
                values);

            // Thông báo đến Customer
            await AddNotificationAsync(
                "TECHNICIAN_ASSIGNED",
                new[] { repair.Customer.UserId },
                repair.RepairRequestId,
                values);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            await ClearHistoryUserAsync();
        }
    }

    // Kỹ thuật viên từ chối công việc được phân công
    public async Task RejectAssignmentAsync(
        int userId,
        int assignmentId,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException(
                "Vui lòng nhập lý do từ chối.");

        reason = reason.Trim();

        if (reason.Length > 500)
            throw new ArgumentException(
                "Lý do từ chối tối đa 500 ký tự.");

        var technician = await EnsureTechnicianAsync(userId);

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var assignment = await _db.RepairAssignment
                .Include(a => a.RepairRequest)
                .FirstOrDefaultAsync(a =>
                    a.RepairAssignmentId == assignmentId &&
                    a.TechnicianId == technician.TechnicianId);

            if (assignment == null)
                throw new KeyNotFoundException(
                    "Không tìm thấy phân công của bạn.");

            if (assignment.AssignmentStatus !=
                (int)RepairAssignmentStatus.Offered)
                throw new InvalidOperationException(
                    "Phân công đã được phản hồi hoặc bị hủy.");

            if (assignment.RepairRequest.Status !=
                (int)RepairRequestStatus.Accepted)
                throw new InvalidOperationException(
                    "Yêu cầu không còn chờ kỹ thuật viên nhận việc.");

            assignment.AssignmentStatus =
                (int)RepairAssignmentStatus.Rejected;
            assignment.RespondedAt = DateTime.UtcNow;
            assignment.RejectionReason = reason;

            // Repair vẫn Accepted để Manager phân công lại.
            // Không chuyển thành Rejected vì Shop đã tiếp nhận.

            var repair = assignment.RepairRequest;

            var managerUserIds = await GetShopManagersAsync(
                repair.ShopId);

            await AddNotificationAsync(
                "ASSIGNMENT_REJECTED",
                managerUserIds,
                repair.RepairRequestId,
                new Dictionary<string, string>
                {
                    ["TechnicianName"] =
                        technician.User.FullName,
                    ["RequestId"] =
                        repair.RepairRequestId.ToString(),
                    ["Reason"] = reason
                });

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // Chỉ truy vấn phân công thuộc tài khoản Technician
    private IQueryable<FixNear.Domain.Models.Repairs.RepairAssignment>
        AssignmentQuery(int userId)
    {
        return _db.RepairAssignment
            .AsNoTracking()
            .Where(a => a.Technician.UserId == userId);
    }

    // Kiểm tra kỹ thuật viên đăng nhập và trạng thái tài khoản
    private async Task<FixNear.Domain.Models.Technicians.TechnicianProfile>
        EnsureTechnicianAsync(int userId)
    {
        var technician = await _db.TechnicianProfile
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.UserId == userId);

        if (technician == null)
            throw new KeyNotFoundException(
                "Không tìm thấy hồ sơ kỹ thuật viên.");

        if (technician.User.Status != 1 ||
            technician.TechnicianStatus != 1)
            throw new UnauthorizedAccessException(
                "Tài khoản kỹ thuật viên không hoạt động.");

        return technician;
    }

    // Lấy Owner và Manager có quyền quản lý Shop
    private async Task<List<int>> GetShopManagersAsync(int shopId)
    {
        return await _db.ShopMember
            .AsNoTracking()
            .Where(sm =>
                sm.ShopId == shopId &&
                sm.Status == ShopMemberStatus.Active &&
                sm.LeftAt == null &&
                (sm.ShopRole == ShopRole.Owner ||
                 sm.ShopRole == ShopRole.Manager) &&
                sm.Manager.User.Status == 1)
            .Select(sm => sm.Manager.UserId)
            .Distinct()
            .ToListAsync();
    }

    // Tạo Notification từ template trong DB
    private async Task AddNotificationAsync(
        string templateCode,
        IEnumerable<int> recipients,
        int repairRequestId,
        IReadOnlyDictionary<string, string> values)
    {
        var userIds = recipients.Distinct().ToList();

        if (userIds.Count == 0)
            throw new InvalidOperationException(
                "Không có người nhận thông báo hợp lệ.");

        var template = await _db.NotificationTemplate
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.TemplateCode == templateCode);

        if (template == null)
            throw new InvalidOperationException(
                $"Chưa cấu hình template: {templateCode}");

        string body = template.BodyTemplate ?? "";

        foreach (var pair in values)
        {
            body = body.Replace(
                "{" + pair.Key + "}",
                pair.Value);
        }

        var now = DateTime.UtcNow;

        var notification = new Notification
        {
            TemplateId = template.NotificationTemplateId,
            Title = template.Title,
            Body = body,
            RefType = (int)NotificationRefType.RepairRequest,
            RefId = repairRequestId,
            CreatedAt = now
        };

        foreach (var id in userIds)
        {
            notification.UserNotification.Add(
                new UserNotification
                {
                    UserId = id,
                    IsRead = false,
                    CreatedAt = now
                });
        }

        _db.Notification.Add(notification);
    }

    // Gắn UserId vào SQL session cho trigger lịch sử
    private async Task SetHistoryUserAsync(int userId)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC sys.sp_set_session_context @key=N'UserId', @value={userId}");
    }

    private async Task ClearHistoryUserAsync()
    {
        await _db.Database.ExecuteSqlRawAsync(
            "EXEC sys.sp_set_session_context @key=N'UserId', @value=NULL");
    }
}
