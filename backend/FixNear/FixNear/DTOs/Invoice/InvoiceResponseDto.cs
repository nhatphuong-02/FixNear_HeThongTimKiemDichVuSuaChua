
namespace FixNear.DTOs.Invoice
{
    public class InvoiceResponseDto
    {
        public int InvoiceId { get; set; }

        public int RepairRequestId { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;

        public int CustomerId { get; set; }

        public int ShopId { get; set; }

        public decimal TotalAmount { get; set; }

        public int Status { get; set; }

        public DateTime IssuedAt { get; set; }

        public DateTime? PaidAt { get; set; }

        public List<InvoiceDetailResponseDto> Details
        { get; set; } = new();
    }

    public class InvoiceDetailResponseDto
    {
        public int InvoiceDetailId { get; set; }

        public string Description { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal Amount { get; set; }
    }
}
