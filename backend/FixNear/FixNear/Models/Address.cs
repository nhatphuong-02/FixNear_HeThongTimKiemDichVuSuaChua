using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class Address
{
    public int AddressId { get; set; }

    public int OwnerType { get; set; }

    public int? CustomerId { get; set; }

    public int ProvinceId { get; set; }

    public int WardId { get; set; }

    public string AddressLine { get; set; } = null!;

    public string? RecipientName { get; set; }

    public string? RecipientPhone { get; set; }

    public bool IsLocked { get; set; }

    public int? SupersededByAddressId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual CustomerProfile? Customer { get; set; }

    public virtual ICollection<CustomerProfile> CustomerProfile { get; set; } = new List<CustomerProfile>();

    public virtual ICollection<Address> InverseSupersededByAddress { get; set; } = new List<Address>();

    public virtual Location? Location { get; set; }

    public virtual Province Province { get; set; } = null!;

    public virtual ICollection<RepairRequest> RepairRequest { get; set; } = new List<RepairRequest>();

    public virtual ICollection<Shop> Shop { get; set; } = new List<Shop>();

    public virtual Address? SupersededByAddress { get; set; }

    public virtual Ward Ward { get; set; } = null!;

    public virtual Ward WardNavigation { get; set; } = null!;
}
