using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class Warranty
{
    public int WarrantyId { get; set; }

    public int RepairRequestId { get; set; }

    public int? InvoiceId { get; set; }

    public string WarrantyCode { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string? Terms { get; set; }

    public int Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Invoice? Invoice { get; set; }

    public virtual RepairRequest RepairRequest { get; set; } = null!;

    public virtual ICollection<WarrantyRequest> WarrantyRequest { get; set; } = new List<WarrantyRequest>();
}
