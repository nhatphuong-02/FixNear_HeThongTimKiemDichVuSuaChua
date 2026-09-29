using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class Location
{
    public int LocationId { get; set; }

    public int AddressId { get; set; }

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Address Address { get; set; } = null!;
}
