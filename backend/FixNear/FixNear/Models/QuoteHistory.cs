using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class QuoteHistory
{
    public int QuoteHistoryId { get; set; }

    public int QuoteId { get; set; }

    public int? ChangedByUserId { get; set; }

    public int? OldStatus { get; set; }

    public int? NewStatus { get; set; }

    public string? ChangeNote { get; set; }

    public decimal? SnapshotTotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? ChangedByUser { get; set; }

    public virtual Quote Quote { get; set; } = null!;
}
