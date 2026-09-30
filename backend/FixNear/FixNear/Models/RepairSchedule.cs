using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class RepairSchedule
{
    public int RepairScheduleId { get; set; }

    public int RepairRequestId { get; set; }

    public int TechnicianId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int Status { get; set; }

    public bool IsCurrent { get; set; }

    public int? CreatedByUserId { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancelReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual RepairRequest RepairRequest { get; set; } = null!;

    public virtual TechnicianProfile Technician { get; set; } = null!;
}
