using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class RepairPart
{
    public int RepairPartId { get; set; }

    public string PartCode { get; set; } = null!;

    public string PartName { get; set; } = null!;

    public string? Description { get; set; }

    public string? Unit { get; set; }

    public decimal CurrentPrice { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<QuoteDetail> QuoteDetail { get; set; } = new List<QuoteDetail>();

    public virtual ICollection<RepairPartUsage> RepairPartUsage { get; set; } = new List<RepairPartUsage>();
}
