
using FixNear.DTOs.Quote;
using FixNear.Enums;
using FixNear.Models;
using FixNear.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Services;

public class QuoteService : IQuoteService
{
    private readonly FixNearDbContext _context;

    public QuoteService(FixNearDbContext context)
    {
        _context = context;
    }

    // X04 - KY THUAT VIEN TAO BAO GIA
    public async Task<CreateQuoteResult> CreateAsync(
        int userId,
        CreateQuoteDto request)
    {
        // 1. Kiem tra du lieu co ban
        if (request.RepairRequestId <= 0 ||
            request.Details == null ||
            request.Details.Count == 0)
        {
            return Fail(CreateQuoteStatus.InvalidData);
        }

        if (request.ValidUntil.HasValue &&
            request.ValidUntil.Value <= DateTime.UtcNow)
        {
            return Fail(CreateQuoteStatus.InvalidData);
        }

        // 2. Kiem tra cong viec thuoc KTV dang nhap
        var assignment = await _context.RepairAssignment
            .Where(a =>
                a.RepairRequestId == request.RepairRequestId &&
                a.Technician.UserId == userId)
            .OrderByDescending(a => a.RepairAssignmentId)
            .FirstOrDefaultAsync();

        if (assignment == null)
            return Fail(CreateQuoteStatus.NotFound);

        // 3. KTV phai da nhan viec
        if (assignment.AssignmentStatus !=
            (int)RepairAssignmentStatus.Accepted)
        {
            return Fail(CreateQuoteStatus.AssignmentNotAccepted);
        }

        // 4. Phai co bao cao khao sat X03
        bool hasSurvey = await _context.RepairReport
            .AnyAsync(r =>
                r.RepairRequestId == request.RepairRequestId &&
                r.TechnicianId == assignment.TechnicianId);

        if (!hasSurvey)
            return Fail(CreateQuoteStatus.SurveyNotFound);

        // 5. Kiem tra tung hang muc va tinh tien
        var details = new List<QuoteDetail>();
        decimal totalAmount = 0m;
        var now = DateTime.UtcNow;

        foreach (var item in request.Details)
        {
            if (item.Quantity <= 0 || item.UnitPrice < 0)
                return Fail(CreateQuoteStatus.InvalidData);

            // Moi ItemType chi duoc co mot tham chieu
            switch (item.ItemType)
            {
                case (int)QuoteItemType.Service:
                    if (!item.ServiceRefId.HasValue ||
                        item.RepairItemRefId.HasValue ||
                        item.RepairPartRefId.HasValue)
                    {
                        return Fail(CreateQuoteStatus.InvalidItem);
                    }

                    if (await _context
                        .Set<FixNear.Models.Service>()
                        .FindAsync(item.ServiceRefId.Value) == null)
                    {
                        return Fail(CreateQuoteStatus.InvalidItem);
                    }
                    break;

                case (int)QuoteItemType.RepairItem:
                    if (!item.RepairItemRefId.HasValue ||
                        item.ServiceRefId.HasValue ||
                        item.RepairPartRefId.HasValue)
                    {
                        return Fail(CreateQuoteStatus.InvalidItem);
                    }

                    if (await _context
                        .Set<RepairItem>()
                        .FindAsync(item.RepairItemRefId.Value) == null)
                    {
                        return Fail(CreateQuoteStatus.InvalidItem);
                    }
                    break;

                case (int)QuoteItemType.RepairPart:
                    if (!item.RepairPartRefId.HasValue ||
                        item.ServiceRefId.HasValue ||
                        item.RepairItemRefId.HasValue)
                    {
                        return Fail(CreateQuoteStatus.InvalidItem);
                    }

                    if (await _context
                        .Set<RepairPart>()
                        .FindAsync(item.RepairPartRefId.Value) == null)
                    {
                        return Fail(CreateQuoteStatus.InvalidItem);
                    }
                    break;

                default:
                    return Fail(CreateQuoteStatus.InvalidItem);
            }

            decimal amount = item.Quantity * item.UnitPrice;
            totalAmount += amount;

            details.Add(new QuoteDetail
            {
                ItemType = item.ItemType,
                ServiceRefId = item.ServiceRefId,
                RepairItemRefId = item.RepairItemRefId,
                RepairPartRefId = item.RepairPartRefId,
                Description = item.Description?.Trim(),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Amount = amount,
                CreatedAt = now
            });
        }

        // 6. Tao thong tin bao gia
        var quote = new FixNear.Models.Quote
        {
            RepairRequestId = request.RepairRequestId,
            TechnicianId = assignment.TechnicianId,
            QuoteNumber = "BG-" +
                Guid.NewGuid().ToString("N")[..16],
            TotalAmount = totalAmount,

            // Gia tri mac dinh hien tai cua DB
            Status = 1,

            CustomerResponse = null,
            ValidUntil = request.ValidUntil,
            CreatedAt = now
        };

        // 7. Them cac chi tiet bao gia
        foreach (var detail in details)
        {
            quote.QuoteDetail.Add(detail);
        }

        // 8. Ghi nhan lich su tao bao gia
        quote.QuoteHistory.Add(new QuoteHistory
        {
            ChangedByUserId = userId,
            OldStatus = null,
            NewStatus = quote.Status,
            ChangeNote = "Tao bao gia moi",
            SnapshotTotalAmount = totalAmount,
            CreatedAt = now
        });

        // 9. Luu Quote, QuoteDetail, QuoteHistory
        // trong mot lan SaveChanges
        _context.Quote.Add(quote);
        await _context.SaveChangesAsync();

        // 10. Tra ket qua
        return new CreateQuoteResult
        {
            Status = CreateQuoteStatus.Success,
            QuoteId = quote.QuoteId,
            TotalAmount = totalAmount
        };
    }

    private static CreateQuoteResult Fail(
        CreateQuoteStatus status)
    {
        return new CreateQuoteResult
        {
            Status = status
        };
    }

    // ============================================
    // X05 - XEM CHI TIET BAO GIA
    // ============================================
    public async Task<QuoteResponseDto?> GetByIdAsync(
        int quoteId,
        int userId,
        int roleType)
    {
        if (quoteId <= 0)
            return null;

        // Chi lay bao gia ma nguoi dung co quyen xem
        var quote = await _context.Quote
            .AsNoTracking()
            .Include(q => q.QuoteDetail)
            .Include(q => q.QuoteHistory)
            .AsSplitQuery()
            .FirstOrDefaultAsync(q =>
                q.QuoteId == quoteId &&
                (
                    // KTV lap bao gia
                    (roleType == (int)RoleType.Technician &&
                     q.Technician.UserId == userId)

                    ||

                    // Quan ly Shop cua yeu cau
                    (roleType == (int)RoleType.Manager &&
                     q.RepairRequest.Shop.Manager.UserId == userId)

                    ||

                    // Khach hang: chi xem bao gia da gui
                    (roleType == (int)RoleType.Customer &&
                     q.RepairRequest.Customer.UserId == userId &&
                     q.Status > 1)
                ));

        if (quote == null)
            return null;

        // Chuyen Entity thanh DTO
        return new QuoteResponseDto
        {
            QuoteId = quote.QuoteId,
            RepairRequestId = quote.RepairRequestId,
            TechnicianId = quote.TechnicianId,
            QuoteNumber = quote.QuoteNumber,
            TotalAmount = quote.TotalAmount,
            Status = quote.Status,
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

                    // Khong tra ghi chu noi bo cho Customer
                    ChangeNote = roleType == (int)RoleType.Customer
                        ? null
                        : h.ChangeNote,

                    SnapshotTotalAmount = h.SnapshotTotalAmount,
                    CreatedAt = h.CreatedAt
                })
                .ToList()
        };
    }

    // ============================================
    // X06 - MANAGER GUI BAO GIA CHO KHACH HANG
    // ============================================
    public async Task<SendQuoteResult> SendAsync(
        int quoteId,
        int userId)
    {
        // 1. Kiem tra bao gia thuoc Shop cua Manager
        var quote = await _context.Quote
            .AsNoTracking()
            .Where(q =>
                q.QuoteId == quoteId &&
                q.RepairRequest.Shop.Manager.UserId == userId)
            .Select(q => new
            {
                q.QuoteId,
                q.Status,
                q.TotalAmount
            })
            .FirstOrDefaultAsync();

        if (quote == null)
            return SendQuoteResult.NotFound;

        // 2. Chi gui bao gia dang o trang thai nhap
        if (quote.Status != (int)QuoteStatus.Draft)
            return SendQuoteResult.InvalidStatus;

        var now = DateTime.UtcNow;

        // 3. Dam bao cap nhat trang thai va lich su
        // duoc luu cung mot giao dich
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        // 4. Cap nhat DRAFT -> SENT
        // Chi cap nhat neu trang thai van la DRAFT
        var affectedRows = await _context.Quote
            .Where(q =>
                q.QuoteId == quoteId &&
                q.Status == (int)QuoteStatus.Draft &&
                q.RepairRequest.Shop.Manager.UserId == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(
                    q => q.Status,
                    (int)QuoteStatus.Sent)
                .SetProperty(
                    q => q.UpdatedAt,
                    now));

        if (affectedRows != 1)
        {
            await transaction.RollbackAsync();
            return SendQuoteResult.InvalidStatus;
        }

        // 5. Ghi lich su gui bao gia
        var history = new QuoteHistory
        {
            QuoteId = quoteId,
            ChangedByUserId = userId,
            OldStatus = (int)QuoteStatus.Draft,
            NewStatus = (int)QuoteStatus.Sent,
            ChangeNote = "Manager gui bao gia cho khach hang",
            SnapshotTotalAmount = quote.TotalAmount,
            CreatedAt = now
        };

        _context.QuoteHistory.Add(history);

        // 6. Luu lich su va hoan tat giao dich
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return SendQuoteResult.Success;

    }

    // ============================================
    // X07 - CUSTOMER CHAP NHAN BAO GIA
    // ============================================
    public async Task<AcceptQuoteResult> AcceptQuoteAsync(
        int quoteId,
        int userId)
    {
        // 1. Kiem tra bao gia thuoc Customer dang dang nhap
        var quote = await _context.Quote
            .AsNoTracking()
            .Where(q =>
                q.QuoteId == quoteId &&
                q.RepairRequest.Customer.UserId == userId)
            .Select(q => new
            {
                q.Status,
                q.CustomerResponse,
                q.ValidUntil,
                q.TotalAmount
            })
            .FirstOrDefaultAsync();

        if (quote == null)
            return AcceptQuoteResult.NotFound;

        // 2. Chi chap nhan bao gia da gui va chua phan hoi
        if (quote.Status != (int)QuoteStatus.Sent ||
            quote.CustomerResponse.HasValue)
        {
            return AcceptQuoteResult.InvalidStatus;
        }

        var now = DateTime.UtcNow;

        // 3. Kiem tra bao gia het han
        if (quote.ValidUntil.HasValue &&
            quote.ValidUntil.Value <= now)
        {
            return AcceptQuoteResult.Expired;
        }

        // 4. Mo giao dich
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        // 5. Cap nhat bao gia neu van du dieu kien
        var affectedRows = await _context.Quote
            .Where(q =>
                q.QuoteId == quoteId &&
                q.RepairRequest.Customer.UserId == userId &&
                q.Status == (int)QuoteStatus.Sent &&
                q.CustomerResponse == null &&
                (!q.ValidUntil.HasValue ||
                 q.ValidUntil.Value > now))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(
                    q => q.Status,
                    (int)QuoteStatus.Accepted)
                .SetProperty(
                    q => q.CustomerResponse,
                    (int)QuoteCustomerResponse.Accepted)
                .SetProperty(
                    q => q.CustomerResponseAt,
                    now)
                .SetProperty(
                    q => q.UpdatedAt,
                    now));

        if (affectedRows != 1)
        {
            await transaction.RollbackAsync();
            return AcceptQuoteResult.InvalidStatus;
        }

        // 6. Luu lich su chap nhan bao gia
        var history = new QuoteHistory
        {
            QuoteId = quoteId,
            ChangedByUserId = userId,
            OldStatus = (int)QuoteStatus.Sent,
            NewStatus = (int)QuoteStatus.Accepted,
            ChangeNote = "Khach hang chap nhan bao gia",
            SnapshotTotalAmount = quote.TotalAmount,
            CreatedAt = now
        };

        _context.QuoteHistory.Add(history);

        // 7. Luu va hoan tat giao dich
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return AcceptQuoteResult.Success;
    }

    // ============================================
    // X08 - CUSTOMER TU CHOI BAO GIA
    // ============================================
    public async Task<RejectQuoteResult> RejectQuoteAsync(
        int quoteId,
        int userId)
    {
        // 1. Kiem tra bao gia thuoc Customer dang dang nhap
        var quote = await _context.Quote
            .AsNoTracking()
            .Where(q =>
                q.QuoteId == quoteId &&
                q.RepairRequest.Customer.UserId == userId)
            .Select(q => new
            {
                q.Status,
                q.CustomerResponse,
                q.ValidUntil,
                q.TotalAmount
            })
            .FirstOrDefaultAsync();

        if (quote == null)
            return RejectQuoteResult.NotFound;

        // 2. Chi tu choi bao gia da gui, chua phan hoi
        if (quote.Status != (int)QuoteStatus.Sent ||
            quote.CustomerResponse.HasValue)
        {
            return RejectQuoteResult.InvalidStatus;
        }

        var now = DateTime.UtcNow;

        // 3. Kiem tra thoi han bao gia
        if (quote.ValidUntil.HasValue &&
            quote.ValidUntil.Value <= now)
        {
            return RejectQuoteResult.Expired;
        }

        // 4. Mo transaction de luu nhat quan
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        // 5. Cap nhat SENT -> REJECTED
        var affectedRows = await _context.Quote
            .Where(q =>
                q.QuoteId == quoteId &&
                q.RepairRequest.Customer.UserId == userId &&
                q.Status == (int)QuoteStatus.Sent &&
                q.CustomerResponse == null &&
                (!q.ValidUntil.HasValue ||
                 q.ValidUntil.Value > now))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(
                    q => q.Status,
                    (int)QuoteStatus.Rejected)
                .SetProperty(
                    q => q.CustomerResponse,
                    (int)QuoteCustomerResponse.Rejected)
                .SetProperty(
                    q => q.CustomerResponseAt,
                    now)
                .SetProperty(
                    q => q.UpdatedAt,
                    now));

        if (affectedRows != 1)
        {
            await transaction.RollbackAsync();
            return RejectQuoteResult.InvalidStatus;
        }

        // 6. Ghi lich su Customer tu choi
        var history = new QuoteHistory
        {
            QuoteId = quoteId,
            ChangedByUserId = userId,
            OldStatus = (int)QuoteStatus.Sent,
            NewStatus = (int)QuoteStatus.Rejected,
            ChangeNote = "Khach hang tu choi bao gia",
            SnapshotTotalAmount = quote.TotalAmount,
            CreatedAt = now
        };

        _context.QuoteHistory.Add(history);

        // 7. Luu lich su va hoan tat transaction
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return RejectQuoteResult.Success;
    }




}
