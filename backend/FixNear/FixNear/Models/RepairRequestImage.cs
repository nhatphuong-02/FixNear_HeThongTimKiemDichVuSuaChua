using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class RepairRequestImage
{
    public int RepairRequestImageId { get; set; }

    public int RepairRequestId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public string? Caption { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual RepairRequest RepairRequest { get; set; } = null!;
}
