using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class Province
{
    public int ProvinceId { get; set; }

    public string ProvinceCode { get; set; } = null!;

    public string ProvinceName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Address> Address { get; set; } = new List<Address>();

    public virtual ICollection<Ward> Ward { get; set; } = new List<Ward>();
}
