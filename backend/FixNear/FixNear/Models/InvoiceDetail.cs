using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class InvoiceDetail
{
    public int InvoiceDetailId { get; set; }

    public int InvoiceId { get; set; }

    public string Description { get; set; } = null!;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;
}
