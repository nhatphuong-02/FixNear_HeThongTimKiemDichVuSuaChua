using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class ShopImage
{
    public int ShopImageId { get; set; }

    public int ShopId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public string? Caption { get; set; }

    public bool IsPrimary { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Shop Shop { get; set; } = null!;
}
