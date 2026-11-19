using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Shops.DTOs
{
    public class UpdateShopProfileRequestDto
    {
        [Required]
        [MaxLength(200)]
        public string ShopName { get; set; } = null!;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        [MaxLength(50)]
        public string? TaxCode { get; set; }
    }
}