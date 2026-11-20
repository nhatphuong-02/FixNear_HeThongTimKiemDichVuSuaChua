using FixNear.Domain.Models.Repairs;

namespace FixNear.Domain.Models.Shops;

public partial class ShopRepairItem
{
    public int ShopRepairItemId { get; set; }

    public int ShopId { get; set; }

    public int RepairItemId { get; set; }

    // Giá do Shop thiết lập
    public decimal Price { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Shop Shop { get; set; } = null!;

    public virtual RepairItem RepairItem { get; set; } = null!;
}
