
using System.Data;
using FixNear.Domain.Enums;
using FixNear.Domain.Models.Notifications;
using FixNear.Domain.Models.Quotes;
using FixNear.Domain.Models.Repairs;
using FixNear.Domain.Models.ServiceCatalog;
using FixNear.Enums;
using FixNear.Features.Quotes.DTOs;
using FixNear.Features.Quotes.Interfaces;
using FixNear.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Quotes.Services;

public sealed class QuoteService : IQuoteService
{
    private readonly FixNearDbContext _db;

    public QuoteService(FixNearDbContext db)
    {
        _db = db;
    }

    // Kỹ thuật viên tạo báo giá sau khảo sát
    public async Task<CreateQuoteResult> CreateAsync(
        int userId, CreateQuoteDto request)
    {
        if (request == null ||
            request.RepairRequestId <= 0 ||
            request.Details == null ||
            request.Details.Count == 0 ||
            request.ValidUntil <= DateTime.UtcNow)
        {
            // ValidUntil = null vẫn được chấp nhận
            if (request == null ||
                request.RepairRequestId <= 0 ||
                request.Details == null ||
                request.Details.Count == 0 ||
                (request.ValidUntil.HasValue &&
                 request.ValidUntil.Value <= DateTime.UtcNow))
                return Fail(CreateQuoteStatus.InvalidData);
        }

        await using var tx = await _db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var assignment = await _db.RepairAssignment
            .AsNoTracking()
            .Where(a =>
                a.RepairRequestId == request.RepairRequestId &&
                a.Technician.UserId == userId)
            .OrderByDescending(a => a.RepairAssignmentId)
            .FirstOrDefaultAsync();

        if (assignment == null)
            return Fail(CreateQuoteStatus.NotFound);

        if (assignment.AssignmentStatus !=
            (int)RepairAssignmentStatus.Accepted)
            return Fail(CreateQuoteStatus.AssignmentNotAccepted);

        var repair = await _db.RepairRequest
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.RepairRequestId == request.RepairRequestId);

        // Báo giá bổ sung chỉ được lập khi Repair đang tiến hành,
        // không thay đổi trạng thái Repair và không đụng báo giá gốc.
        if (repair == null ||
            (repair.Status != (int)RepairRequestStatus.Inspecting &&
             repair.Status != (int)RepairRequestStatus.Repairing))
            return Fail(CreateQuoteStatus.InvalidData);

        bool supplemental = repair.Status == (int)RepairRequestStatus.Repairing;
        if (supplemental && !await _db.Quote.AnyAsync(q =>
            q.RepairRequestId == request.RepairRequestId &&
            !q.IsSupplemental && q.Status == (int)QuoteStatus.Accepted &&
            q.CustomerResponse == (int)QuoteCustomerResponse.Accepted))
            return Fail(CreateQuoteStatus.InvalidData);

        bool hasReport = await _db.RepairReport
            .AnyAsync(r =>
                r.RepairRequestId == request.RepairRequestId &&
                r.TechnicianId == assignment.TechnicianId);

        if (!hasReport)
            return Fail(CreateQuoteStatus.SurveyNotFound);

        bool existingOpenQuote = supplemental
            ? await _db.Quote.AnyAsync(q =>
                q.RepairRequestId == request.RepairRequestId &&
                q.IsSupplemental &&
                (q.Status == (int)QuoteStatus.Draft ||
                 q.Status == (int)QuoteStatus.Sent))
            : await _db.Quote.AnyAsync(q =>
                q.RepairRequestId == request.RepairRequestId &&
                !q.IsSupplemental &&
                q.Status != (int)QuoteStatus.Rejected);

        if (existingOpenQuote)
            return Fail(CreateQuoteStatus.InvalidData);

        // Kiểm tra từng hạng mục báo giá
        var details = new List<QuoteDetail>();
        decimal total = 0;
        var now = DateTime.UtcNow;


        foreach (var item in request.Details)
        {
            if (item.Quantity <= 0 ||
                item.Quantity > 9999999999999999.99m ||
                item.Description?.Length > 500)
                return Fail(CreateQuoteStatus.InvalidData);

            decimal? unitPrice = null;
            bool validReferences = false;

            switch ((QuoteItemType)item.ItemType)
            {
                case QuoteItemType.Service:
                    {
                        validReferences =
                            item.ServiceRefId.HasValue &&
                            !item.RepairItemRefId.HasValue &&
                            !item.RepairPartRefId.HasValue;

                        if (!validReferences)
                            break;

                        int serviceId = item.ServiceRefId!.Value;

                        // Chỉ lấy giá dịch vụ của Shop đang xử lý
                        var nowPrice = DateTime.UtcNow;

                        unitPrice = await _db.ServicePrice
                            .AsNoTracking()
                            .Where(sp =>
                                sp.ShopService.ShopId == repair.ShopId &&
                                sp.ShopService.ServiceId == serviceId &&
                                sp.ShopService.IsActive &&
                                sp.ShopService.Service.IsActive &&
                                sp.IsActive &&
                                sp.EffectiveFrom <= nowPrice &&
                                (sp.EffectiveTo == null ||
                                 sp.EffectiveTo > nowPrice))
                            .Select(sp => (decimal?)sp.Price)
                            .FirstOrDefaultAsync();

                        break;
                    }

                case QuoteItemType.RepairItem:
                    {
                        validReferences =
                            item.RepairItemRefId.HasValue &&
                            !item.ServiceRefId.HasValue &&
                            !item.RepairPartRefId.HasValue;

                        if (!validReferences)
                            break;

                        int repairItemId = item.RepairItemRefId!.Value;

                        // Giá công sửa chữa do Shop thiết lập
                        unitPrice = await _db.ShopRepairItem
                            .AsNoTracking()
                            .Where(si =>
                                si.ShopId == repair.ShopId &&
                                si.RepairItemId == repairItemId &&
                                si.IsActive &&
                                si.RepairItem.IsActive)
                            .Select(si => (decimal?)si.Price)
                            .FirstOrDefaultAsync();

                        break;
                    }

                case QuoteItemType.RepairPart:
                    {
                        validReferences =
                            item.RepairPartRefId.HasValue &&
                            !item.ServiceRefId.HasValue &&
                            !item.RepairItemRefId.HasValue;

                        if (!validReferences)
                            break;

                        int repairPartId = item.RepairPartRefId!.Value;

                        // Giá linh kiện do Shop thiết lập
                        unitPrice = await _db.ShopRepairPart
                            .AsNoTracking()
                            .Where(sp =>
                                sp.ShopId == repair.ShopId &&
                                sp.RepairPartId == repairPartId &&
                                sp.IsActive &&
                                sp.RepairPart.IsActive)
                            .Select(sp => (decimal?)sp.Price)
                            .FirstOrDefaultAsync();

                        break;
                    }

                default:
                    return Fail(CreateQuoteStatus.InvalidItem);
            }

            if (!validReferences ||
                !unitPrice.HasValue ||
                unitPrice.Value < 0)
                return Fail(CreateQuoteStatus.InvalidItem);

            decimal amount;

            try
            {
                amount = Math.Round(
                    checked(item.Quantity * unitPrice.Value),
                    2,
                    MidpointRounding.AwayFromZero);

                total = checked(total + amount);
            }
            catch (OverflowException)
            {
                return Fail(CreateQuoteStatus.InvalidData);
            }

            details.Add(new QuoteDetail
            {
                ItemType = item.ItemType,
                ServiceRefId = item.ServiceRefId,
                RepairItemRefId = item.RepairItemRefId,
                RepairPartRefId = item.RepairPartRefId,
                Description = item.Description?.Trim(),
                Quantity = item.Quantity,

                // Giá được tính ở Backend, không lấy từ request
                UnitPrice = unitPrice.Value,

                CreatedAt = now
            });
        }


        if (total > 9999999999999999.99m)
            return Fail(CreateQuoteStatus.InvalidData);

        var quote = new Quote
        {
            RepairRequestId = request.RepairRequestId,
            TechnicianId = assignment.TechnicianId,
            QuoteNumber = "BG-" +
                Guid.NewGuid().ToString("N")[..16],
            TotalAmount = total,
            Status = (int)QuoteStatus.Draft,
            IsSupplemental = supplemental,
            ValidUntil = request.ValidUntil,
            CreatedAt = now
        };

        foreach (var detail in details)
            quote.QuoteDetail.Add(detail);

        quote.QuoteHistory.Add(new QuoteHistory
        {
            ChangedByUserId = userId,
            OldStatus = null,
            NewStatus = (int)QuoteStatus.Draft,
            ChangeNote = supplemental
                ? "Kỹ thuật viên tạo báo giá phát sinh"
                : "Kỹ thuật viên tạo báo giá",
            SnapshotTotalAmount = total,
            CreatedAt = now
        });

        _db.Quote.Add(quote);
        await _db.SaveChangesAsync();

        // Thông báo Manager kiểm tra báo giá
        var managerIds = await GetShopManagersAsync(repair.ShopId);

        var technicianName = await _db.TechnicianProfile
            .Where(t => t.TechnicianId == assignment.TechnicianId)
            .Select(t => t.User.FullName)
            .FirstAsync();

        await AddNotificationAsync(
            "QUOTE_CREATED",
            managerIds,
            quote.QuoteId,
            new Dictionary<string, string>
            {
                ["TechnicianName"] = technicianName,
                ["QuoteNumber"] = quote.QuoteNumber,
                ["RequestId"] = repair.RepairRequestId.ToString(),
                ["TotalAmount"] = FormatAmount(total)
            });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return new CreateQuoteResult
        {
            Status = CreateQuoteStatus.Success,
            QuoteId = quote.QuoteId,
            TotalAmount = total
        };
    }

    // Xem chi tiết báo giá theo quyền người đăng nhập
    public async Task<QuoteResponseDto?> GetByIdAsync(
        int quoteId, int userId, int roleType)
    {
        if (quoteId <= 0)
            return null;

        var query = _db.Quote
            .AsNoTracking()
            .AsSplitQuery()
            .Include(q => q.QuoteDetail)
            .Include(q => q.QuoteHistory)
            .Where(q => q.QuoteId == quoteId);

        query = roleType switch
        {
            (int)RoleType.Technician =>
                query.Where(q => q.Technician.UserId == userId),

            (int)RoleType.Manager =>
                query.Where(q => _db.ShopMember.Any(sm =>
                    sm.ShopId == q.RepairRequest.ShopId &&
                    sm.Manager.UserId == userId &&
                    sm.Status == ShopMemberStatus.Active &&
                    sm.LeftAt == null &&
                    (sm.ShopRole == ShopRole.Owner ||
                     sm.ShopRole == ShopRole.Manager))),

            (int)RoleType.Customer =>
                query.Where(q =>
                    q.RepairRequest.Customer.UserId == userId &&
                    q.Status != (int)QuoteStatus.Draft),

            _ => query.Where(q => false)
        };

        var quote = await query.FirstOrDefaultAsync();

        if (quote == null)
            return null;

        return new QuoteResponseDto
        {
            QuoteId = quote.QuoteId,
            RepairRequestId = quote.RepairRequestId,
            TechnicianId = quote.TechnicianId,
            QuoteNumber = quote.QuoteNumber,
            TotalAmount = quote.TotalAmount,
            Status = quote.Status,
            IsSupplemental = quote.IsSupplemental,
            CustomerResponse = quote.CustomerResponse,
            CustomerResponseAt = quote.CustomerResponseAt,
            ValidUntil = quote.ValidUntil,
            CreatedAt = quote.CreatedAt,

            Details = quote.QuoteDetail
                .OrderBy(d => d.QuoteDetailId)
                .Select(d => new QuoteDetailResponseDto
                {
                    QuoteDetailId = d.QuoteDetailId,
                    ItemType = d.ItemType,
                    ServiceRefId = d.ServiceRefId,
                    RepairItemRefId = d.RepairItemRefId,
                    RepairPartRefId = d.RepairPartRefId,
                    Description = d.Description,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Amount = d.Amount
                })
                .ToList(),

            History = quote.QuoteHistory
                .OrderBy(h => h.QuoteHistoryId)
                .Select(h => new QuoteHistoryResponseDto
                {
                    QuoteHistoryId = h.QuoteHistoryId,
                    ChangedByUserId = h.ChangedByUserId,
                    OldStatus = h.OldStatus,
                    NewStatus = h.NewStatus,
                    ChangeNote =
                        roleType == (int)RoleType.Customer
                            ? null
                            : h.ChangeNote,
                    SnapshotTotalAmount = h.SnapshotTotalAmount,
                    CreatedAt = h.CreatedAt
                })
                .ToList()
        };
    }

    // Manager kiểm tra và gửi báo giá tới Customer
    public async Task<SendQuoteResult> SendAsync(
        int quoteId, int userId)
    {
        await using var tx = await _db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var quote = await _db.Quote
            .Include(q => q.RepairRequest)
            .FirstOrDefaultAsync(q =>
                q.QuoteId == quoteId &&
                _db.ShopMember.Any(sm =>
                    sm.ShopId == q.RepairRequest.ShopId &&
                    sm.Manager.UserId == userId &&
                    sm.Status == ShopMemberStatus.Active &&
                    sm.LeftAt == null &&
                    (sm.ShopRole == ShopRole.Owner ||
                     sm.ShopRole == ShopRole.Manager)));

        if (quote == null)
            return SendQuoteResult.NotFound;

        if (quote.Status != (int)QuoteStatus.Draft ||
            quote.RepairRequest.Status !=
                (quote.IsSupplemental
                    ? (int)RepairRequestStatus.Repairing
                    : (int)RepairRequestStatus.Inspecting) ||
            (quote.ValidUntil.HasValue &&
             quote.ValidUntil.Value <= DateTime.UtcNow))
            return SendQuoteResult.InvalidStatus;

        var customerUserId = await _db.CustomerProfile
            .Where(c => c.CustomerId == quote.RepairRequest.CustomerId)
            .Select(c => c.UserId)
            .FirstAsync();

        var now = DateTime.UtcNow;

        quote.Status = (int)QuoteStatus.Sent;
        quote.UpdatedAt = now;

        quote.QuoteHistory.Add(new QuoteHistory
        {
            ChangedByUserId = userId,
            OldStatus = (int)QuoteStatus.Draft,
            NewStatus = (int)QuoteStatus.Sent,
            ChangeNote = "Manager gửi báo giá cho khách hàng",
            SnapshotTotalAmount = quote.TotalAmount,
            CreatedAt = now
        });

        // Ghi người thao tác cho trigger Repair
        await SetHistoryUserAsync(userId);

        try
        {
            if (!quote.IsSupplemental)
            {
                quote.RepairRequest.Status =
                    (int)RepairRequestStatus.Quoted;
                quote.RepairRequest.UpdatedAt = now;
            }

            await AddNotificationAsync(
                "QUOTE_UPDATED",
                new[] { customerUserId },
                quote.QuoteId,
                QuoteValues(quote));

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return SendQuoteResult.Success;
        }
        finally
        {
            await ClearHistoryUserAsync();
        }
    }

    // Customer chấp nhận báo giá
    public async Task<AcceptQuoteResult> AcceptQuoteAsync(
        int quoteId, int userId)
    {
        await using var tx = await _db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var quote = await _db.Quote
            .Include(q => q.RepairRequest)
            .FirstOrDefaultAsync(q =>
                q.QuoteId == quoteId &&
                q.RepairRequest.Customer.UserId == userId);

        if (quote == null)
            return AcceptQuoteResult.NotFound;

        if (quote.Status != (int)QuoteStatus.Sent ||
            quote.CustomerResponse.HasValue ||
            quote.RepairRequest.Status !=
                (quote.IsSupplemental
                    ? (int)RepairRequestStatus.Repairing
                    : (int)RepairRequestStatus.Quoted))
            return AcceptQuoteResult.InvalidStatus;

        var now = DateTime.UtcNow;

        if (quote.ValidUntil.HasValue &&
            quote.ValidUntil.Value <= now)
            return AcceptQuoteResult.Expired;

        quote.Status = (int)QuoteStatus.Accepted;
        quote.CustomerResponse =
            (int)QuoteCustomerResponse.Accepted;
        quote.CustomerResponseAt = now;
        quote.UpdatedAt = now;

        quote.QuoteHistory.Add(new QuoteHistory
        {
            ChangedByUserId = userId,
            OldStatus = (int)QuoteStatus.Sent,
            NewStatus = (int)QuoteStatus.Accepted,
            ChangeNote = "Customer chấp nhận báo giá",
            SnapshotTotalAmount = quote.TotalAmount,
            CreatedAt = now
        });

        var recipients = await GetShopManagersAsync(
            quote.RepairRequest.ShopId);

        var technicianUserId = await _db.TechnicianProfile
            .Where(t => t.TechnicianId == quote.TechnicianId)
            .Select(t => t.UserId)
            .FirstAsync();

        recipients.Add(technicianUserId);

        var customerName = await _db.CustomerProfile
            .Where(c => c.UserId == userId)
            .Select(c => c.User.FullName)
            .FirstAsync();

        await SetHistoryUserAsync(userId);

        try
        {
            if (!quote.IsSupplemental)
            {
                quote.RepairRequest.Status =
                    (int)RepairRequestStatus.QuoteApproved;
                quote.RepairRequest.UpdatedAt = now;
            }

            var values = QuoteValues(quote);
            values["CustomerName"] = customerName;

            await AddNotificationAsync(
                "QUOTE_ACCEPTED",
                recipients,
                quote.QuoteId,
                values);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return AcceptQuoteResult.Success;
        }
        finally
        {
            await ClearHistoryUserAsync();
        }
    }

    // Customer từ chối báo giá
    public async Task<RejectQuoteResult> RejectQuoteAsync(
        int quoteId, int userId)
    {
        await using var tx = await _db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var quote = await _db.Quote
            .Include(q => q.RepairRequest)
            .FirstOrDefaultAsync(q =>
                q.QuoteId == quoteId &&
                q.RepairRequest.Customer.UserId == userId);

        if (quote == null)
            return RejectQuoteResult.NotFound;

        if (quote.Status != (int)QuoteStatus.Sent ||
            quote.CustomerResponse.HasValue ||
            quote.RepairRequest.Status !=
                (quote.IsSupplemental
                    ? (int)RepairRequestStatus.Repairing
                    : (int)RepairRequestStatus.Quoted))
            return RejectQuoteResult.InvalidStatus;

        var now = DateTime.UtcNow;

        if (quote.ValidUntil.HasValue &&
            quote.ValidUntil.Value <= now)
            return RejectQuoteResult.Expired;

        quote.Status = (int)QuoteStatus.Rejected;
        quote.CustomerResponse =
            (int)QuoteCustomerResponse.Rejected;
        quote.CustomerResponseAt = now;
        quote.UpdatedAt = now;

        quote.QuoteHistory.Add(new QuoteHistory
        {
            ChangedByUserId = userId,
            OldStatus = (int)QuoteStatus.Sent,
            NewStatus = (int)QuoteStatus.Rejected,
            ChangeNote = "Customer từ chối báo giá",
            SnapshotTotalAmount = quote.TotalAmount,
            CreatedAt = now
        });

        var recipients = await GetShopManagersAsync(
            quote.RepairRequest.ShopId);

        var technicianUserId = await _db.TechnicianProfile
            .Where(t => t.TechnicianId == quote.TechnicianId)
            .Select(t => t.UserId)
            .FirstAsync();

        recipients.Add(technicianUserId);

        var customerName = await _db.CustomerProfile
            .Where(c => c.UserId == userId)
            .Select(c => c.User.FullName)
            .FirstAsync();

        await SetHistoryUserAsync(userId);

        try
        {
            // Cho phép KTV lập báo giá thay thế.
            // Báo cáo khảo sát trước đó vẫn được giữ.
            if (!quote.IsSupplemental)
            {
                quote.RepairRequest.Status =
                    (int)RepairRequestStatus.Inspecting;
                quote.RepairRequest.UpdatedAt = now;
            }

            var values = QuoteValues(quote);
            values["CustomerName"] = customerName;

            await AddNotificationAsync(
                "QUOTE_REJECTED",
                recipients,
                quote.QuoteId,
                values);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return RejectQuoteResult.Success;
        }
        finally
        {
            await ClearHistoryUserAsync();
        }
    }

    // Tạo thông báo từ template có sẵn
    private async Task AddNotificationAsync(
        string templateCode,
        IEnumerable<int> recipients,
        int quoteId,
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
                $"Thiếu NotificationTemplate: {templateCode}");

        var title = template.Title;
        var body = template.BodyTemplate ?? "";

        foreach (var item in values)
        {
            title = title.Replace(
                "{" + item.Key + "}", item.Value);

            body = body.Replace(
                "{" + item.Key + "}", item.Value);
        }

        var now = DateTime.UtcNow;

        var notification = new Notification
        {
            TemplateId = template.NotificationTemplateId,
            Title = title,
            Body = body,
            RefType = (int)NotificationRefType.Quote,
            RefId = quoteId,
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

    // Lấy toàn bộ Owner và Manager hợp lệ của Shop
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

    // Chuẩn bị biến cho Notification Template
    private static Dictionary<string, string> QuoteValues(
        Quote quote)
    {
        return new Dictionary<string, string>
        {
            ["QuoteNumber"] = quote.QuoteNumber,
            ["RequestId"] = quote.RepairRequestId.ToString(),
            ["TotalAmount"] = FormatAmount(quote.TotalAmount)
        };
    }

    private static string FormatAmount(decimal amount)
    {
        return amount.ToString(
            "N0",
            System.Globalization.CultureInfo.GetCultureInfo("vi-VN"))
            + " đ";
    }

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

    private static CreateQuoteResult Fail(CreateQuoteStatus status)
    {
        return new CreateQuoteResult { Status = status };
    }
}
