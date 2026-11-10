
using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.RepairItem
{
    public class CreateRepairItemDto
    {
        [Required(ErrorMessage = "Mã hạng mục không được để trống")]
        public string ItemCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên hạng mục không được để trống")]
        public string ItemName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(typeof(decimal), "0", "999999999999",
            ErrorMessage = "Giá mặc định không được âm")]
        public decimal DefaultPrice { get; set; }
    }
}
