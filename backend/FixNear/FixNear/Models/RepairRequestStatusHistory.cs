using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class RepairRequestStatusHistory
{
    public int HistoryId { get; set; }

    public int RepairRequestId { get; set; }

    public int? OldStatus { get; set; }

    public int NewStatus { get; set; }

    public int? ChangedByUserId { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? ChangedByUser { get; set; }

    public virtual RepairRequest RepairRequest { get; set; } = null!;
}
