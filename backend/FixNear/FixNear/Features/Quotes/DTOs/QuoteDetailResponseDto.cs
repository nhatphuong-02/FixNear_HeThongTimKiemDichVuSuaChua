namespace FixNear.Features.Quotes.DTOs
{
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
}
