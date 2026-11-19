
using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Manager.DTOs;

public class ShopImageDto
{
    [Required(ErrorMessage = "Vui lòng nhập đường dẫn ảnh.")]
    [Url(ErrorMessage = "Đường dẫn ảnh không hợp lệ.")]
    [StringLength(500, ErrorMessage = "Đường dẫn ảnh không được vượt quá 500 ký tự.")]
    public string ImageUrl { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Mô tả ảnh không được vượt quá 200 ký tự.")]
    public string? Caption { get; set; }
}
