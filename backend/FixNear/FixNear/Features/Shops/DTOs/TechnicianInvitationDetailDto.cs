namespace FixNear.Features.Shops.DTOs
{
    public class TechnicianInvitationDetailDto
    {
        public int InvitationId { get; set; }
        public string Email { get; set; } = null!;
        public string ShopName { get; set; } = null!;
        public string InvitedByName { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public int Status { get; set; }
    }
}
