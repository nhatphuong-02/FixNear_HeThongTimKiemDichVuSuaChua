
using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Invoice
{
    public class CreateInvoiceDto
    {
        [Range(1, int.MaxValue,
            ErrorMessage = "Mã yêu cầu sửa chữa không hợp lệ")]
        public int RepairRequestId { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Mã báo giá không hợp lệ")]
        public int QuoteId { get; set; }
    }
}
