using FixNear.Enums;
using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Shops.DTOs
{
    public class UpdateShopMemberRequestDto
    {
        [Required]
        public ShopRole ShopRole { get; set; }
    }
}
