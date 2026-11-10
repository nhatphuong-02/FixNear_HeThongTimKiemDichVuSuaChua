
namespace FixNear.DTOs.Quote;

public class QuoteResponseDto
{
    public int QuoteId { get; set; }
    public int RepairRequestId { get; set; }
    public int TechnicianId { get; set; }

    public string QuoteNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int Status { get; set; }

    public int? CustomerResponse { get; set; }
    public DateTime? CustomerResponseAt { get; set; }
    public DateTime? ValidUntil { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<QuoteDetailResponseDto> Details { get; set; }
        = new();

    public List<QuoteHistoryResponseDto> History { get; set; }
        = new();
}

public class QuoteDetailResponseDto
{
    public int QuoteDetailId { get; set; }
    public int ItemType { get; set; }

    public int? ServiceRefId { get; set; }
    public int? RepairItemRefId { get; set; }
    public int? RepairPartRefId { get; set; }

    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}

public class QuoteHistoryResponseDto
{
    public int QuoteHistoryId { get; set; }
    public int? ChangedByUserId { get; set; }

    public int? OldStatus { get; set; }
    public int? NewStatus { get; set; }

    public string? ChangeNote { get; set; }
    public decimal? SnapshotTotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
