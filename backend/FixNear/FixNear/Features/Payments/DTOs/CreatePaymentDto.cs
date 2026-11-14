using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Payments.DTOs
{
    public class CreatePaymentDto
    {
        [Range(1, int.MaxValue,
            ErrorMessage = "Mã hóa đơn không hợp lệ")]
        public int InvoiceId { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Phương thức thanh toán không hợp lệ")]
        public int PaymentMethod { get; set; }
    }
}
