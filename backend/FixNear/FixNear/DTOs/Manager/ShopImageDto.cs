using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Manager;

public class ShopImageDto
{
    [Required]
    [Url(ErrorMessage = "Đường dẫn ảnh phải là URL hợp lệ")]
    [StringLength(500)]
    public string ImageUrl { get; set; } = "";

    [StringLength(200)]
    public string? Caption { get; set; }
}
