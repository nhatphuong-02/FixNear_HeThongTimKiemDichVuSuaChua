
namespace FixNear.Features.Shops.DTOs
{
    public class TechnicianInvitationResponseDto
    {
        public int InvitationId { get; set; }

        public int ShopId { get; set; }

        public int? TechnicianId { get; set; }

        public string Email { get; set; } = null!;

        public string? TechnicianName { get; set; }

        public string InvitedByName { get; set; } = null!;

        public int Status { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? RespondedAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
