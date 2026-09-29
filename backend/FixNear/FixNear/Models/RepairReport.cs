using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class RepairReport
{
    public int RepairReportId { get; set; }

    public int RepairRequestId { get; set; }

    public int TechnicianId { get; set; }

    public string? Diagnosis { get; set; }

    public string? WorkDescription { get; set; }

    public string? Result { get; set; }

    public string? Recommendation { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual RepairRequest RepairRequest { get; set; } = null!;

    public virtual TechnicianProfile Technician { get; set; } = null!;
}
