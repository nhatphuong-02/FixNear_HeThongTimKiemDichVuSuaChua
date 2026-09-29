using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class RepairPartUsage
{
    public int RepairPartUsageId { get; set; }

    public int RepairRequestId { get; set; }

    public int RepairPartId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPriceAtUsage { get; set; }

    public decimal Amount { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual RepairPart RepairPart { get; set; } = null!;

    public virtual RepairRequest RepairRequest { get; set; } = null!;
}
