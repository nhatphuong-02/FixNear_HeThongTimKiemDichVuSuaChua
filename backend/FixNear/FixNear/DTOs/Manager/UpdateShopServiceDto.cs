using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Manager
{
    public class UpdateShopServiceDto
    {
        [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự")]
        public string? Description { get; set; }
    }
}
