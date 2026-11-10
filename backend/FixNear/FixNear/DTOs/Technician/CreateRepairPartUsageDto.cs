
using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Technician
{
    public class CreateRepairPartUsageDto
    {
        [Range(1, int.MaxValue,
            ErrorMessage = "Mã yêu cầu sửa chữa không hợp lệ")]
        public int RepairRequestId { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Mã linh kiện không hợp lệ")]
        public int RepairPartId { get; set; }

        [Range(typeof(decimal), "0.01", "999999",
            ErrorMessage = "Số lượng phải lớn hơn 0")]
        public decimal Quantity { get; set; }

        [MaxLength(500,
            ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string? Note { get; set; }
    }
}
