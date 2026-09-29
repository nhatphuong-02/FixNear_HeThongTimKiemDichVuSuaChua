using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class Quote
{
    public int QuoteId { get; set; }

    public int RepairRequestId { get; set; }

    public int TechnicianId { get; set; }

    public string QuoteNumber { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public int Status { get; set; }

    public int? CustomerResponse { get; set; }

    public DateTime? CustomerResponseAt { get; set; }

    public DateTime? ValidUntil { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<QuoteDetail> QuoteDetail { get; set; } = new List<QuoteDetail>();

    public virtual ICollection<QuoteHistory> QuoteHistory { get; set; } = new List<QuoteHistory>();

    public virtual RepairRequest RepairRequest { get; set; } = null!;

    public virtual TechnicianProfile Technician { get; set; } = null!;
}
