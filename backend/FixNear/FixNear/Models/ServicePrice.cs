using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class ServicePrice
{
    public int ServicePriceId { get; set; }

    public int ShopServiceId { get; set; }

    public decimal Price { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public string? PriceUnit { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ShopService ShopService { get; set; } = null!;
}
