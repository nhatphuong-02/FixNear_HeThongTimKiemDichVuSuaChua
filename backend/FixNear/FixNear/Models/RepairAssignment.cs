using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class RepairAssignment
{
    public int RepairAssignmentId { get; set; }

    public int RepairRequestId { get; set; }

    public int TechnicianId { get; set; }

    public int? AssignedByUserId { get; set; }

    public int AssignmentStatus { get; set; }

    public DateTime AssignedAt { get; set; }

    public DateTime? RespondedAt { get; set; }

    public string? RejectionReason { get; set; }

    public string? Note { get; set; }

    public virtual User? AssignedByUser { get; set; }

    public virtual RepairRequest RepairRequest { get; set; } = null!;

    public virtual TechnicianProfile Technician { get; set; } = null!;
}
