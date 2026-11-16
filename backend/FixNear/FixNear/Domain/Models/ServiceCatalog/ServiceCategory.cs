using System;
using System.Collections.Generic;

namespace FixNear.Domain.Models.ServiceCatalog;

public partial class ServiceCategory
{
    public int ServiceCategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Service> Service { get; set; } = new List<Service>();
}
