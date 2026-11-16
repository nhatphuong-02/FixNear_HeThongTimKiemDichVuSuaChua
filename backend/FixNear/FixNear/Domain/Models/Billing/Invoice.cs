using FixNear.Domain.Models.Customers;
using FixNear.Domain.Models.Repairs;
using FixNear.Domain.Models.Shops;
using FixNear.Domain.Models.Warranties;
using System;
using System.Collections.Generic;

namespace FixNear.Domain.Models.Billing;

public partial class Invoice
{
    public int InvoiceId { get; set; }

    public int RepairRequestId { get; set; }

    public string InvoiceNumber { get; set; } = null!;

    public int CustomerId { get; set; }

    public int ShopId { get; set; }

    public decimal TotalAmount { get; set; }

    public int Status { get; set; }

    public DateTime IssuedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public virtual CustomerProfile Customer { get; set; } = null!;

    public virtual ICollection<InvoiceDetail> InvoiceDetail { get; set; } = new List<InvoiceDetail>();

    public virtual ICollection<Payment> Payment { get; set; } = new List<Payment>();

    public virtual RepairRequest RepairRequest { get; set; } = null!;

    public virtual RepairRequest RepairRequestNavigation { get; set; } = null!;

    public virtual Shop Shop { get; set; } = null!;

    public virtual ICollection<Warranty> Warranty { get; set; } = new List<Warranty>();
}
