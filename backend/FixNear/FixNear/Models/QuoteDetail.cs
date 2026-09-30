using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class QuoteDetail
{
    public int QuoteDetailId { get; set; }

    public int QuoteId { get; set; }

    public int ItemType { get; set; }

    public int? ServiceRefId { get; set; }

    public int? RepairItemRefId { get; set; }

    public int? RepairPartRefId { get; set; }

    public string? Description { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Quote Quote { get; set; } = null!;

    public virtual RepairItem? RepairItemRef { get; set; }

    public virtual RepairPart? RepairPartRef { get; set; }

    public virtual Service? ServiceRef { get; set; }
}
