using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class TechnicianShop
{
    public int TechnicianShopId { get; set; }

    public int TechnicianId { get; set; }

    public int ShopId { get; set; }

    public DateTime JoinedAt { get; set; }

    public DateTime? LeftAt { get; set; }

    public int Status { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Shop Shop { get; set; } = null!;

    public virtual TechnicianProfile Technician { get; set; } = null!;
}
