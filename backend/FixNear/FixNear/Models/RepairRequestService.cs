using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class RepairRequestService
{
    public int RepairRequestServiceId { get; set; }

    public int RepairRequestId { get; set; }

    public int ServiceId { get; set; }

    public string? Description { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual RepairRequest RepairRequest { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;
}
