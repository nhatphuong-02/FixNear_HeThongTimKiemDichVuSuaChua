using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class Ward
{
    public int WardId { get; set; }

    public int ProvinceId { get; set; }

    public string WardCode { get; set; } = null!;

    public string WardName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Address> AddressWard { get; set; } = new List<Address>();

    public virtual ICollection<Address> AddressWardNavigation { get; set; } = new List<Address>();

    public virtual Province Province { get; set; } = null!;
}
