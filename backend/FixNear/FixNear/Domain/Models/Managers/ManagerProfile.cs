using FixNear.Domain.Models.Identity;
using FixNear.Domain.Models.Shops;
using System;
using System.Collections.Generic;

namespace FixNear.Domain.Models.Managers;

public partial class ManagerProfile
{
    public int ManagerId { get; set; }

    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Shop> Shop { get; set; } = new List<Shop>();

    public virtual User User { get; set; } = null!;

    public virtual ICollection<ShopMember> ShopMembers { get; set; } = new List<ShopMember>();
}
