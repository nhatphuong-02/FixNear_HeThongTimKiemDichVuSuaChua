using System;
using System.Collections.Generic;

namespace FixNear.Domain.Models.Billing;

public partial class PaymentTransaction
{
    public int PaymentTransactionId { get; set; }

    public int PaymentId { get; set; }

    public string? TransactionCode { get; set; }

    public int Status { get; set; }

    public string? Message { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Payment Payment { get; set; } = null!;
}
