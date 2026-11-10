using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Manager;

public class UpdateShopDto
{
    [Required(ErrorMessage = "Tên cửa hàng không được để trống")]
    [StringLength(200)]
    public string ShopName { get; set; } = "";

    [StringLength(2000)]
    public string? Description { get; set; }

    [Phone]
    [StringLength(20)]
    public string? Phone { get; set; }

    [EmailAddress]
    [StringLength(200)]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? LogoUrl { get; set; }
}
