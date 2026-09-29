using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class WarrantyRequest
{
    public int WarrantyRequestId { get; set; }

    public int WarrantyId { get; set; }

    public int CustomerId { get; set; }

    public string IssueDescription { get; set; } = null!;

    public int Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual CustomerProfile Customer { get; set; } = null!;

    public virtual Warranty Warranty { get; set; } = null!;

    public virtual ICollection<WarrantyHistory> WarrantyHistory { get; set; } = new List<WarrantyHistory>();
}
