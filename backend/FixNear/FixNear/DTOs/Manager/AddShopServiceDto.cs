using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Manager
{
    public class AddShopServiceDto
    {
        [Required(ErrorMessage = "Vui lòng chọn dịch vụ")]
        [Range(1, int.MaxValue, ErrorMessage = "ServiceId không hợp lệ")]
        public int ServiceId { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự")]
        public string? Description { get; set; }
    }
}
