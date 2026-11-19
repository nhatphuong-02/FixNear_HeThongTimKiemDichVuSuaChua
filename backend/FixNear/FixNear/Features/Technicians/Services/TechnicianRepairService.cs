
using System.Data;
using FixNear.Domain.Enums;
using FixNear.Domain.Models.Notifications;
using FixNear.Domain.Models.Repairs;
using FixNear.Features.Technicians.DTOs;
using FixNear.Features.Technicians.Interfaces;
using FixNear.Infrastructure.Persistence;
using FixNear.Enums;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Technicians.Services;

public sealed class TechnicianRepairService
    : ITechnicianRepairService
{
    private readonly FixNearDbContext _db;

    public TechnicianRepairService(FixNearDbContext db)
    {
        _db = db;
    }


    // Lấy Customer và các Owner/Manager đang quản lý Shop
    private async Task<List<int>> GetRepairRecipientsAsync(
        RepairRequest repair)
    {
        var customerUserId = await _db.CustomerProfile
            .Where(c => c.CustomerId == repair.CustomerId)
            .Select(c => c.UserId)
            .FirstAsync();

        var managerUserIds = await _db.ShopMember
            .AsNoTracking()
            .Where(sm =>
                sm.ShopId == repair.ShopId &&
                sm.Status == ShopMemberStatus.Active &&
                sm.LeftAt == null &&
                (sm.ShopRole == ShopRole.Owner ||
                 sm.ShopRole == ShopRole.Manager) &&
                sm.Manager.User.Status == 1)
            .Select(sm => sm.Manager.UserId)
            .Distinct()
            .ToListAsync();

        managerUserIds.Add(customerUserId);

        return managerUserIds.Distinct().ToList();
    }

    // Kiểm tra quyền thao tác Repair của Technician
    private async Task<RepairAssignment>
        GetAcceptedAssignmentAsync(
            int userId,
            int repairRequestId)
    {
        var assignment = await _db.RepairAssignment
            .Include(a => a.Technician)
                .ThenInclude(t => t.User)
            .Include(a => a.RepairRequest)
            .Where(a =>
                a.RepairRequestId == repairRequestId &&
                a.Technician.UserId == userId &&
                a.AssignmentStatus ==
                    (int)RepairAssignmentStatus.Accepted)
            .OrderByDescending(a => a.RepairAssignmentId)
            .FirstOrDefaultAsync();

        if (assignment == null)
            throw new KeyNotFoundException(
                "Bạn chưa nhận phân công cho yêu cầu này.");

        if (assignment.Technician.User.Status != 1 ||
            assignment.Technician.TechnicianStatus != 1)
            throw new UnauthorizedAccessException(
                "Tài khoản kỹ thuật viên không hoạt động.");

        bool isActiveMember = await _db.TechnicianShop
            .AnyAsync(ts =>
                ts.ShopId == assignment.RepairRequest.ShopId &&
                ts.TechnicianId == assignment.TechnicianId &&
                ts.Status == TechnicianShopStatus.Active &&
                ts.LeftAt == null);

        if (!isActiveMember)
            throw new UnauthorizedAccessException(
                "Bạn không còn thuộc cửa hàng này.");

        return assignment;
    }

    // Gắn tài khoản đang thao tác cho SQL trigger
    private async Task SetHistoryUserAsync(int userId)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC sys.sp_set_session_context @key=N'UserId', @value={userId}");
    }

    // Xóa UserId trong SQL session
    private async Task ClearHistoryUserAsync()
    {
        await _db.Database.ExecuteSqlRawAsync(
            "EXEC sys.sp_set_session_context @key=N'UserId', @value=NULL");
    }
    // Tạo thông báo Repair từ template có sẵn trong DB
    private async Task AddRepairNotificationAsync(
        string templateCode,
        IEnumerable<int> recipients,
        int repairRequestId,
        IReadOnlyDictionary<string, string> values)
    {
        var receiverIds = recipients.Distinct().ToList();

        if (receiverIds.Count == 0)
            throw new InvalidOperationException(
                "Không có người nhận thông báo.");

        var template = await _db.NotificationTemplate
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.TemplateCode == templateCode);

        if (template == null)
            throw new InvalidOperationException(
                $"Thiếu mẫu thông báo: {templateCode}");

        string title = template.Title;
        string body = template.BodyTemplate ?? string.Empty;

        foreach (var item in values)
        {
            string key = "{" + item.Key + "}";

            title = title.Replace(key, item.Value);
            body = body.Replace(key, item.Value);
        }

        var now = DateTime.UtcNow;

        var notification = new Notification
        {
            TemplateId = template.NotificationTemplateId,
            Title = title,
            Body = body,
            RefType = (int)NotificationRefType.RepairRequest,
            RefId = repairRequestId,
            CreatedAt = now
        };

        foreach (var receiverId in receiverIds)
        {
            notification.UserNotification.Add(
                new UserNotification
                {
                    UserId = receiverId,
                    IsRead = false,
                    CreatedAt = now
                });
        }

        _db.Notification.Add(notification);
    }


    // Bắt đầu khảo sát sau khi nhận phân công
    public async Task StartSurveyAsync(
        int userId,
        int repairRequestId)
    {
        if (repairRequestId <= 0)
            throw new ArgumentException(
                "Mã yêu cầu không hợp lệ.");

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var assignment = await GetAcceptedAssignmentAsync(
                userId, repairRequestId);

            var repair = assignment.RepairRequest;

            if (repair.Status != (int)RepairRequestStatus.Assigned)
                throw new InvalidOperationException(
                    "Yêu cầu chưa sẵn sàng để khảo sát.");

            if (await _db.RepairReport.AnyAsync(r =>
                r.RepairRequestId == repairRequestId))
                throw new InvalidOperationException(
                    "Yêu cầu đã có báo cáo khảo sát.");

            await SetHistoryUserAsync(userId);

            repair.Status = (int)RepairRequestStatus.Inspecting;
            repair.UpdatedAt = DateTime.UtcNow;

            // SQL trigger tự ghi lịch sử trạng thái
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

    // Lưu kết quả khảo sát và thông báo cho Shop
    public async Task<int> CompleteSurveyAsync(
        int userId,
        int repairRequestId,
        CreateSurveyReportDto dto)
    {
        if (repairRequestId <= 0)
            throw new ArgumentException(
                "Mã yêu cầu không hợp lệ.");

        if (dto == null ||
            string.IsNullOrWhiteSpace(dto.Diagnosis))
            throw new ArgumentException(
                "Vui lòng nhập kết quả chẩn đoán.");

        string diagnosis = dto.Diagnosis.Trim();
        string? recommendation = dto.Recommendation?.Trim();

        if (diagnosis.Length < 5 ||
            diagnosis.Length > 2000 ||
            recommendation?.Length > 1000)
            throw new ArgumentException(
                "Nội dung khảo sát vượt giới hạn cho phép.");

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var assignment = await GetAcceptedAssignmentAsync(
                userId, repairRequestId);

            var repair = assignment.RepairRequest;

            if (repair.Status != (int)RepairRequestStatus.Inspecting)
                throw new InvalidOperationException(
                    "Yêu cầu chưa ở trạng thái đang khảo sát.");

            bool reportExists = await _db.RepairReport
                .AnyAsync(r =>
                    r.RepairRequestId == repairRequestId);

            if (reportExists)
                throw new InvalidOperationException(
                    "Yêu cầu đã có báo cáo khảo sát.");

            var now = DateTime.UtcNow;

            var report = new RepairReport
            {
                RepairRequestId = repairRequestId,
                TechnicianId = assignment.TechnicianId,
                Diagnosis = diagnosis,
                Recommendation = recommendation,
                CreatedAt = now
            };

            _db.RepairReport.Add(report);

            // Lấy người nhận thông báo của Shop
            var managerUserIds = await _db.ShopMember
                .AsNoTracking()
                .Where(sm =>
                    sm.ShopId == repair.ShopId &&
                    sm.Status == ShopMemberStatus.Active &&
                    sm.LeftAt == null &&
                    (sm.ShopRole == ShopRole.Owner ||
                     sm.ShopRole == ShopRole.Manager) &&
                    sm.Manager.User.Status == 1)
                .Select(sm => sm.Manager.UserId)
                .Distinct()
                .ToListAsync();

            if (managerUserIds.Count == 0)
                throw new InvalidOperationException(
                    "Shop không có người quản lý hợp lệ.");

            var template = await _db.NotificationTemplate
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.TemplateCode == "SURVEY_COMPLETED");

            if (template == null)
                throw new InvalidOperationException(
                    "Chưa cấu hình template SURVEY_COMPLETED.");

            var technicianName = assignment.Technician.User.FullName;

            string body = (template.BodyTemplate ?? "")
                .Replace("{TechnicianName}", technicianName)
                .Replace(
                    "{RequestId}",
                    repairRequestId.ToString());

            // Notification tham chiếu đến RepairRequest
            var notification = new Notification
            {
                TemplateId = template.NotificationTemplateId,
                Title = template.Title,
                Body = body,
                RefType = (int)NotificationRefType.RepairRequest,
                RefId = repairRequestId,
                CreatedAt = now
            };

            foreach (var managerUserId in managerUserIds)
            {
                notification.UserNotification.Add(
                    new UserNotification
                    {
                        UserId = managerUserId,
                        IsRead = false,
                        CreatedAt = now
                    });
            }

            _db.Notification.Add(notification);

            // Lưu báo cáo và thông báo chung transaction
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return report.RepairReportId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // Bắt đầu sửa chữa sau khi báo giá được Customer chấp nhận
    public async Task StartRepairAsync(
        int userId,
        int repairRequestId)
    {
        if (repairRequestId <= 0)
            throw new ArgumentException("Mã yêu cầu không hợp lệ.");

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var assignment = await GetAcceptedAssignmentAsync(
                userId, repairRequestId);

            var repair = assignment.RepairRequest;

            if (repair.Status !=
                (int)RepairRequestStatus.QuoteApproved)
                throw new InvalidOperationException(
                    "Yêu cầu chưa được duyệt báo giá để sửa chữa.");

            bool hasApprovedQuote = await _db.Quote
                .AnyAsync(q =>
                    q.RepairRequestId == repairRequestId &&
                    q.TechnicianId == assignment.TechnicianId &&
                    q.Status == (int)QuoteStatus.Accepted &&
                    q.CustomerResponse ==
                        (int)QuoteCustomerResponse.Accepted);

            if (!hasApprovedQuote)
                throw new InvalidOperationException(
                    "Không tìm thấy báo giá đã được khách hàng duyệt.");

            bool hasSurveyReport = await _db.RepairReport
                .AnyAsync(r =>
                    r.RepairRequestId == repairRequestId &&
                    r.TechnicianId == assignment.TechnicianId &&
                    r.Diagnosis != null);

            if (!hasSurveyReport)
                throw new InvalidOperationException(
                    "Chưa có báo cáo khảo sát hợp lệ.");

            var now = DateTime.UtcNow;

            repair.Status = (int)RepairRequestStatus.Repairing;
            repair.UpdatedAt = now;

            await SetHistoryUserAsync(userId);

            // Trigger SQL tự ghi lịch sử trạng thái
            await _db.SaveChangesAsync();

            // Thông báo cho Customer và người quản lý Shop
            var recipients = await GetRepairRecipientsAsync(repair);

            await AddRepairNotificationAsync(
                "REPAIR_STARTED",
                recipients,
                repairRequestId,
                new Dictionary<string, string>
                {
                    ["TechnicianName"] =
                        assignment.Technician.User.FullName,
                    ["RequestId"] = repairRequestId.ToString()
                });

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


    // Ghi nhận linh kiện sử dụng theo báo giá đã được duyệt
    public async Task<int> RecordPartUsageAsync(
        int userId,
        int repairRequestId,
        CreateRepairPartUsageDto dto)
    {
        if (dto == null ||
            repairRequestId <= 0 ||
            dto.RepairRequestId != repairRequestId ||
            dto.RepairPartId <= 0 ||
            dto.Quantity <= 0 ||
            decimal.Round(dto.Quantity, 2) != dto.Quantity)
            throw new ArgumentException(
                "Dữ liệu linh kiện hoặc số lượng không hợp lệ.");

        if (dto.Note?.Length > 500)
            throw new ArgumentException(
                "Ghi chú không được quá 500 ký tự.");

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var assignment = await GetAcceptedAssignmentAsync(
                userId, repairRequestId);

            if (assignment.RepairRequest.Status !=
                (int)RepairRequestStatus.Repairing)
                throw new InvalidOperationException(
                    "Chỉ được ghi nhận linh kiện khi đang sửa chữa.");

            var part = await _db.RepairPart
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.RepairPartId == dto.RepairPartId &&
                    p.IsActive);

            if (part == null)
                throw new KeyNotFoundException(
                    "Linh kiện không tồn tại hoặc đã ngừng sử dụng.");

            // Tổng hợp số lượng được duyệt từ tất cả báo giá.
            // Có thể có báo giá gốc + nhiều báo giá bổ sung.
            var approvedQuotes = await _db.Quote
                .AsNoTracking()
                .Where(q =>
                    q.RepairRequestId == repairRequestId &&
                    q.Status == (int)QuoteStatus.Accepted &&
                    q.CustomerResponse == (int)QuoteCustomerResponse.Accepted)
                .Select(q => q.QuoteId)
                .ToListAsync();

            if (approvedQuotes.Count == 0)
                throw new InvalidOperationException(
                    "Chưa có báo giá được Customer duyệt.");

            var quotedParts = await _db.QuoteDetail
                .AsNoTracking()
                .Where(d =>
                    approvedQuotes.Contains(d.QuoteId) &&
                    d.ItemType == (int)QuoteItemType.RepairPart &&
                    d.RepairPartRefId == dto.RepairPartId)
                .Select(d => new { d.Quantity, d.UnitPrice })
                .ToListAsync();

            if (quotedParts.Count == 0)
                throw new InvalidOperationException(
                    "Linh kiện chưa có trong báo giá được duyệt.");

            // Chặn các mức giá không nhất quán cho cùng linh kiện;
            // bản nâng cấp tiếp theo sẽ lưu QuoteDetailId trên usage để phân bổ chính xác.
            var prices = quotedParts.Select(x => x.UnitPrice).Distinct().ToList();
            if (prices.Count != 1 || prices[0] < 0)
                throw new InvalidOperationException(
                    "Một linh kiện có nhiều mức giá được duyệt; cần chọn QuoteDetail cụ thể.");

            decimal approvedQuantity = quotedParts.Sum(x => x.Quantity);

            decimal usedQuantity =
                await _db.RepairPartUsage
                    .Where(u =>
                        u.RepairRequestId == repairRequestId &&
                        u.RepairPartId == dto.RepairPartId)
                    .SumAsync(u => (decimal?)u.Quantity) ?? 0m;

            if (usedQuantity + dto.Quantity > approvedQuantity)
                throw new InvalidOperationException(
                    "Số lượng sử dụng vượt báo giá đã được duyệt.");

            // Lấy đơn giá từ báo giá, không lấy CurrentPrice
            var usage = new RepairPartUsage
            {
                RepairRequestId = repairRequestId,
                RepairPartId = dto.RepairPartId,
                Quantity = dto.Quantity,
                UnitPriceAtUsage = prices[0],
                Note = dto.Note?.Trim(),
                CreatedAt = DateTime.UtcNow

                // Amount được SQL tự tính
            };

            _db.RepairPartUsage.Add(usage);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();

            return usage.RepairPartUsageId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // Hoàn thành sửa chữa và cập nhật báo cáo kỹ thuật
    public async Task CompleteRepairAsync(
        int userId,
        int repairRequestId,
        CompleteRepairDto dto)
    {
        if (repairRequestId <= 0 ||
            dto == null ||
            string.IsNullOrWhiteSpace(dto.Result))
            throw new ArgumentException(
                "Vui lòng nhập kết quả sửa chữa hợp lệ.");

        var result = dto.Result.Trim();
        var workDescription = dto.WorkDescription?.Trim();
        var recommendation = dto.Recommendation?.Trim();

        if (result.Length > 1000 ||
            workDescription?.Length > 2000 ||
            recommendation?.Length > 1000)
            throw new ArgumentException(
                "Nội dung báo cáo vượt độ dài cho phép.");

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var assignment = await GetAcceptedAssignmentAsync(
                userId, repairRequestId);

            var repair = assignment.RepairRequest;

            if (repair.Status !=
                (int)RepairRequestStatus.Repairing)
                throw new InvalidOperationException(
                    "Yêu cầu không ở trạng thái đang sửa chữa.");

            bool approvedQuoteExists = await _db.Quote.AnyAsync(q =>
                q.RepairRequestId == repairRequestId &&
                q.TechnicianId == assignment.TechnicianId &&
                q.Status == (int)QuoteStatus.Accepted &&
                q.CustomerResponse ==
                    (int)QuoteCustomerResponse.Accepted);

            if (!approvedQuoteExists)
                throw new InvalidOperationException(
                    "Yêu cầu chưa có báo giá được duyệt.");

            bool hasPendingSupplement = await _db.Quote.AnyAsync(q =>
                q.RepairRequestId == repairRequestId &&
                q.IsSupplemental &&
                (q.Status == (int)QuoteStatus.Draft ||
                 q.Status == (int)QuoteStatus.Sent));

            if (hasPendingSupplement)
                throw new InvalidOperationException(
                    "Có báo giá bổ sung chưa được xử lý; không thể báo hoàn thành.");

            var report = await _db.RepairReport
                .Where(r =>
                    r.RepairRequestId == repairRequestId &&
                    r.TechnicianId == assignment.TechnicianId)
                .OrderByDescending(r => r.RepairReportId)
                .FirstOrDefaultAsync();

            if (report == null ||
                string.IsNullOrWhiteSpace(report.Diagnosis))
                throw new InvalidOperationException(
                    "Chưa có báo cáo khảo sát hợp lệ.");

            if (!string.IsNullOrWhiteSpace(report.Result))
                throw new InvalidOperationException(
                    "Báo cáo đã có kết quả hoàn thành.");

            var now = DateTime.UtcNow;

            report.Result = result;
            report.WorkDescription = workDescription;
            report.Recommendation = recommendation;
            report.UpdatedAt = now;

            repair.Status = (int)RepairRequestStatus.Completed;
            repair.CompletedAt = now;
            repair.UpdatedAt = now;

            await SetHistoryUserAsync(userId);

            // SQL trigger ghi Repairing -> Completed
            await _db.SaveChangesAsync();

            var recipients = await _db.ShopMember.AsNoTracking()
                .Where(sm => sm.ShopId == repair.ShopId &&
                    sm.Status == ShopMemberStatus.Active &&
                    sm.LeftAt == null &&
                    (sm.ShopRole == ShopRole.Owner ||
                     sm.ShopRole == ShopRole.Manager))
                .Select(sm => sm.Manager.UserId)
                .Distinct().ToListAsync();

            await AddRepairNotificationAsync(
                "REPAIR_AWAITING_ACCEPTANCE",
                recipients,
                repairRequestId,
                new Dictionary<string, string>
                {
                    ["RequestId"] = repairRequestId.ToString(),
                    ["TechnicianName"] =
                        assignment.Technician.User.FullName
                });

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

}
