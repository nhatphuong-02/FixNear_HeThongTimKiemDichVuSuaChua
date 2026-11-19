using FixNear.Domain.Models.Repairs;

namespace FixNear.Domain.Models.Shops;

public partial class ShopRepairPart
{
    public int ShopRepairPartId { get; set; }

    public int ShopId { get; set; }

    public int RepairPartId { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Shop Shop { get; set; } = null!;

    public virtual RepairPart RepairPart { get; set; } = null!;
}
