using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class RepairItem
{
    public int RepairItemId { get; set; }

    public string ItemCode { get; set; } = null!;

    public string ItemName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal DefaultPrice { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<QuoteDetail> QuoteDetail { get; set; } = new List<QuoteDetail>();
}
