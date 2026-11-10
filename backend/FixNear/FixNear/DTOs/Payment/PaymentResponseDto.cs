
namespace FixNear.DTOs.Payment
{
    public class PaymentResponseDto
    {
        public int PaymentId { get; set; }

        public int InvoiceId { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;

        public int PaymentMethod { get; set; }

        public decimal Amount { get; set; }

        public int Status { get; set; }

        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public List<PaymentTransactionResponseDto> Transactions
        { get; set; } = new();
    }

    public class PaymentTransactionResponseDto
    {
        public int PaymentTransactionId { get; set; }

        public string? TransactionCode { get; set; }

        public int Status { get; set; }

        public string? Message { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
