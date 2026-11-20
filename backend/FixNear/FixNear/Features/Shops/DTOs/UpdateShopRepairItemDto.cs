using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Shops.DTOs;

public sealed class UpdateShopRepairItemDto
{
    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal Price { get; set; }

    public bool IsActive { get; set; }
}
