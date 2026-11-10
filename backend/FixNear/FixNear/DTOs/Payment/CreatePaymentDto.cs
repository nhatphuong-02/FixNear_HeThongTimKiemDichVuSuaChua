
using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Payment
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
