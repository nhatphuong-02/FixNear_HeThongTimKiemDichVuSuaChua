using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class WarrantyHistory
{
    public int WarrantyHistoryId { get; set; }

    public int WarrantyRequestId { get; set; }

    public int? ChangedByUserId { get; set; }

    public int? OldStatus { get; set; }

    public int NewStatus { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? ChangedByUser { get; set; }

    public virtual WarrantyRequest WarrantyRequest { get; set; } = null!;
}
