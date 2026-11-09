
using FixNear.DTOs.Invoice;

namespace FixNear.Services.Interfaces
{
    public interface IInvoiceService
    {
        // X12 - Tao hoa don
        Task<CreateInvoiceResult> CreateAsync(
            CreateInvoiceDto dto,
            int userId);

        // X13 - Xem chi tiet hoa don
        Task<GetInvoiceResult> GetByIdAsync(
            int invoiceId,
            int userId,
            int roleType);
    }

    public enum CreateInvoiceStatus
    {
        Success,
        InvalidInput,
        NotFound,
        InvalidRequestStatus,
        QuoteNotAccepted,
        EmptyQuote,
        InvalidQuoteDetail,
        AlreadyExists
    }

    public class CreateInvoiceResult
    {
        public CreateInvoiceStatus Status { get; set; }

        public int? InvoiceId { get; set; }

        public string? InvoiceNumber { get; set; }

        public decimal? TotalAmount { get; set; }
    }

    public enum GetInvoiceStatus
    {
        Success,
        NotFound
    }

    public class GetInvoiceResult
    {
        public GetInvoiceStatus Status { get; set; }

        public InvoiceResponseDto? Data { get; set; }
    }
}
