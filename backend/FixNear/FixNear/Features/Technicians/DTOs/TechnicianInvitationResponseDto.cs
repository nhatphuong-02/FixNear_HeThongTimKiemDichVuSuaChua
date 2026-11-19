namespace FixNear.Features.Technicians.DTOs;

public class TechnicianInvitationResponseDto
{
    public int InvitationId { get; set; }

    public int ShopId { get; set; }

    public string ShopName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string InvitedByName { get; set; } = null!;

    public int Status { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RespondedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
