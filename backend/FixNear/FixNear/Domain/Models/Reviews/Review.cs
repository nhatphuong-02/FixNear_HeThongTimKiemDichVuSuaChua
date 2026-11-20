using FixNear.Domain.Models.Customers;
using FixNear.Domain.Models.Repairs;
using FixNear.Domain.Models.Shops;
using FixNear.Domain.Models.Technicians;

namespace FixNear.Domain.Models.Reviews;

public partial class Review
{
    public int ReviewId { get; set; }

    public int RepairRequestId { get; set; }

    public int CustomerId { get; set; }

    public int ShopId { get; set; }

    public int? TechnicianId { get; set; }

    public int ReviewType { get; set; } = 1;

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public int Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual CustomerProfile Customer { get; set; } = null!;

    public virtual RepairRequest RepairRequest { get; set; } = null!;

    public virtual RepairRequest RepairRequestNavigation { get; set; } = null!;

    public virtual ICollection<ReviewImage> ReviewImage { get; set; } = new List<ReviewImage>();

    public virtual ICollection<ReviewReply> ReviewReply { get; set; } = new List<ReviewReply>();

    public virtual Shop Shop { get; set; } = null!;

    public virtual TechnicianProfile? Technician { get; set; }
}
