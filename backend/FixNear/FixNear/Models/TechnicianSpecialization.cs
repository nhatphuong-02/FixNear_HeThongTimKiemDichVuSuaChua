using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class TechnicianSpecialization
{
    public int TechnicianSpecializationId { get; set; }

    public int TechnicianId { get; set; }

    public int ServiceId { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Service Service { get; set; } = null!;

    public virtual TechnicianProfile Technician { get; set; } = null!;
}
