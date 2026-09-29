using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class RepairRequest
{
    public int RepairRequestId { get; set; }

    public int CustomerId { get; set; }

    public int ShopId { get; set; }

    public int? AddressId { get; set; }

    public int RepairMethod { get; set; }

    public string? Description { get; set; }

    public DateOnly? PreferredDate { get; set; }

    public TimeOnly? PreferredTimeFrom { get; set; }

    public TimeOnly? PreferredTimeTo { get; set; }

    public int Status { get; set; }

    public int Priority { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public virtual Address? Address { get; set; }

    public virtual ICollection<Conversation> Conversation { get; set; } = new List<Conversation>();

    public virtual CustomerProfile Customer { get; set; } = null!;

    public virtual ICollection<Invoice> InvoiceRepairRequest { get; set; } = new List<Invoice>();

    public virtual ICollection<Invoice> InvoiceRepairRequestNavigation { get; set; } = new List<Invoice>();

    public virtual ICollection<Quote> Quote { get; set; } = new List<Quote>();

    public virtual ICollection<RepairAssignment> RepairAssignment { get; set; } = new List<RepairAssignment>();

    public virtual ICollection<RepairPartUsage> RepairPartUsage { get; set; } = new List<RepairPartUsage>();

    public virtual ICollection<RepairReport> RepairReport { get; set; } = new List<RepairReport>();

    public virtual ICollection<RepairRequestImage> RepairRequestImage { get; set; } = new List<RepairRequestImage>();

    public virtual ICollection<RepairRequestService> RepairRequestService { get; set; } = new List<RepairRequestService>();

    public virtual ICollection<RepairRequestStatusHistory> RepairRequestStatusHistory { get; set; } = new List<RepairRequestStatusHistory>();

    public virtual RepairSchedule? RepairSchedule { get; set; }

    public virtual Review? ReviewRepairRequest { get; set; }

    public virtual ICollection<Review> ReviewRepairRequestNavigation { get; set; } = new List<Review>();

    public virtual Shop Shop { get; set; } = null!;

    public virtual ICollection<Warranty> Warranty { get; set; } = new List<Warranty>();
}
