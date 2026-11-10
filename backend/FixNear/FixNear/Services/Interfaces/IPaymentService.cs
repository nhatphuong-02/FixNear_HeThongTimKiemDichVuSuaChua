
using FixNear.DTOs.Payment;

namespace FixNear.Services.Interfaces
{
    public interface IPaymentService
    {
        // X14 - Tao yeu cau thanh toan
        Task<CreatePaymentResult> CreateAsync(
            CreatePaymentDto dto,
            int userId);
        // X15 - Xem thong tin thanh toan
        Task<GetPaymentResult> GetByIdAsync(
            int paymentId,
            int userId,
            int roleType);
    }

    public enum CreatePaymentStatus
    {
        Success,
        InvalidInput,
        InvoiceNotFound,
        InvalidAmount,
        AlreadyPaid,
        PaymentAlreadyExists
    }

    public class CreatePaymentResult
    {
        public CreatePaymentStatus Status { get; set; }

        public int? PaymentId { get; set; }

        public int? InvoiceId { get; set; }

        public decimal? Amount { get; set; }

        public int? PaymentMethod { get; set; }

        public int? PaymentStatus { get; set; }
    }

    public enum GetPaymentStatus
    {
        Success,
        NotFound
    }

    public class GetPaymentResult
    {
        public GetPaymentStatus Status { get; set; }

        public PaymentResponseDto? Data { get; set; }
    }


}
