using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class Service
{
    public int ServiceId { get; set; }

    public int CategoryId { get; set; }

    public string ServiceName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ServiceCategory Category { get; set; } = null!;

    public virtual ICollection<QuoteDetail> QuoteDetail { get; set; } = new List<QuoteDetail>();

    public virtual ICollection<RepairRequestService> RepairRequestService { get; set; } = new List<RepairRequestService>();

    public virtual ICollection<ShopService> ShopService { get; set; } = new List<ShopService>();

    public virtual ICollection<TechnicianSpecialization> TechnicianSpecialization { get; set; } = new List<TechnicianSpecialization>();
}
