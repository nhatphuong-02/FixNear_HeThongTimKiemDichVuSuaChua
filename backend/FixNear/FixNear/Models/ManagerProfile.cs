using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class ManagerProfile
{
    public int ManagerId { get; set; }

    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Shop> Shop { get; set; } = new List<Shop>();

    public virtual User User { get; set; } = null!;
}
