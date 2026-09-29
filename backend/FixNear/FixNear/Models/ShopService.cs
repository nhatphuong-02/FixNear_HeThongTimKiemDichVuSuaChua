using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class ShopService
{
    public int ShopServiceId { get; set; }

    public int ShopId { get; set; }

    public int ServiceId { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Service Service { get; set; } = null!;

    public virtual ServicePrice? ServicePrice { get; set; }

    public virtual Shop Shop { get; set; } = null!;
}
