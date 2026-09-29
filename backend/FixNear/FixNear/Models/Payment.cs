using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int InvoiceId { get; set; }

    public int PaymentMethod { get; set; }

    public decimal Amount { get; set; }

    public int Status { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual ICollection<PaymentTransaction> PaymentTransaction { get; set; } = new List<PaymentTransaction>();
}
