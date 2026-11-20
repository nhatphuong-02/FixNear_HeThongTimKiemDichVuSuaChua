namespace FixNear.Features.Quotes.DTOs;

public class QuoteResponseDto
{
    public int QuoteId { get; set; }
    public int RepairRequestId { get; set; }
    public int TechnicianId { get; set; }

    public string QuoteNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int Status { get; set; }
    public bool IsSupplemental { get; set; }

    public int? CustomerResponse { get; set; }
    public DateTime? CustomerResponseAt { get; set; }
    public DateTime? ValidUntil { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<QuoteDetailResponseDto> Details { get; set; }
        = new();

    public List<QuoteHistoryResponseDto> History { get; set; }
        = new();
}


