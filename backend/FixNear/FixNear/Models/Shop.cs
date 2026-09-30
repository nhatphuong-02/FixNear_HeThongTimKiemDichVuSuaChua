using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class Shop
{
    public int ShopId { get; set; }

    public int ManagerId { get; set; }

    public int AddressId { get; set; }

    public string ShopName { get; set; } = null!;

    public string? Description { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? LogoUrl { get; set; }

    public string? TaxCode { get; set; }

    public decimal RatingAverage { get; set; }

    public int Status { get; set; }

    public bool IsVerified { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Address Address { get; set; } = null!;

    public virtual ICollection<Invoice> Invoice { get; set; } = new List<Invoice>();

    public virtual ManagerProfile Manager { get; set; } = null!;

    public virtual ICollection<RepairRequest> RepairRequest { get; set; } = new List<RepairRequest>();

    public virtual ICollection<Review> Review { get; set; } = new List<Review>();

    public virtual ShopImage? ShopImage { get; set; }

    public virtual ICollection<ShopService> ShopService { get; set; } = new List<ShopService>();

    public virtual ICollection<TechnicianShop> TechnicianShop { get; set; } = new List<TechnicianShop>();
}
