namespace FixNear.Features.Quotes.DTOs
{
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
}
