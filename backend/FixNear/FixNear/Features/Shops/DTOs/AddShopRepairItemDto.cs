using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Shops.DTOs;

public sealed class AddShopRepairItemDto
{
    [Range(1, int.MaxValue)]
    public int RepairItemId { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal Price { get; set; }
}
