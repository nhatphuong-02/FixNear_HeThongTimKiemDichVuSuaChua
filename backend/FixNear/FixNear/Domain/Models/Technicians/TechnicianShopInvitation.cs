using FixNear.Domain.Enums;
using FixNear.Domain.Models.Managers;
using FixNear.Domain.Models.Shops;

namespace FixNear.Domain.Models.Technicians;

public partial class TechnicianShopInvitation
{
    public int InvitationId { get; set; }

    public int ShopId { get; set; }

    public int? TechnicianId { get; set; }

    public string Email { get; set; } = null!;

    public int InvitedBy { get; set; }

    public string? TokenHash { get; set; }

    public TechnicianShopInvitationStatus Status { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RespondedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Shop Shop { get; set; } = null!;

    public virtual TechnicianProfile? Technician { get; set; }

    public virtual ManagerProfile InvitedByNavigation { get; set; } = null!;
}