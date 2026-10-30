
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
}
