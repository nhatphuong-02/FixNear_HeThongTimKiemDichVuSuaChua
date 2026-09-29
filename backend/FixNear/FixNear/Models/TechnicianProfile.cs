using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class TechnicianProfile
{
    public int TechnicianId { get; set; }

    public int UserId { get; set; }

    public int? ExperienceYears { get; set; }

    public string? Introduction { get; set; }

    public int TechnicianStatus { get; set; }

    public decimal RatingAverage { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Quote> Quote { get; set; } = new List<Quote>();

    public virtual ICollection<RepairAssignment> RepairAssignment { get; set; } = new List<RepairAssignment>();

    public virtual ICollection<RepairReport> RepairReport { get; set; } = new List<RepairReport>();

    public virtual ICollection<RepairSchedule> RepairSchedule { get; set; } = new List<RepairSchedule>();

    public virtual ICollection<Review> Review { get; set; } = new List<Review>();

    public virtual TechnicianShop? TechnicianShop { get; set; }

    public virtual ICollection<TechnicianSpecialization> TechnicianSpecialization { get; set; } = new List<TechnicianSpecialization>();

    public virtual User User { get; set; } = null!;
}
