using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Auth.DTOs;

public class RegisterShopRequestDto : RegisterAccountRequestDto
{
    [Required]
    [MaxLength(255)]
    public string ShopName { get; set; } = null!;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public string? ShopPhone { get; set; }

    [EmailAddress]
    [MaxLength(255)]
    public string? ShopEmail { get; set; }

    [MaxLength(50)]
    public string? TaxCode { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn tỉnh/thành phố")]
    public int ProvinceId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn phường/xã")]
    public int WardId { get; set; }

    [Required]
    [MaxLength(500)]
    public string AddressLine { get; set; } = null!;

    public string? RecipientName { get; set; }

    public string? RecipientPhone { get; set; }
}