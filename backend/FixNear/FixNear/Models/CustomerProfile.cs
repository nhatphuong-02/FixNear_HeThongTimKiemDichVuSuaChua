using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class CustomerProfile
{
    public int CustomerId { get; set; }

    public int UserId { get; set; }

    public int? DefaultAddressId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Address> Address { get; set; } = new List<Address>();

    public virtual Address? AddressNavigation { get; set; }

    public virtual ICollection<Invoice> Invoice { get; set; } = new List<Invoice>();

    public virtual ICollection<RepairRequest> RepairRequest { get; set; } = new List<RepairRequest>();

    public virtual ICollection<Review> Review { get; set; } = new List<Review>();

    public virtual User User { get; set; } = null!;

    public virtual ICollection<WarrantyRequest> WarrantyRequest { get; set; } = new List<WarrantyRequest>();
}
