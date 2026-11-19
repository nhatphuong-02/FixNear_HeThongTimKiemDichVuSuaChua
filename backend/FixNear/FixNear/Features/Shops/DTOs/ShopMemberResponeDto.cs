using FixNear.Enums;

namespace FixNear.Features.Shops.DTOs
{
    public class ShopMemberResponseDto
    {
        public int ShopMemberId { get; set; }

        public int ManagerId { get; set; }

        public int UserId { get; set; }

        public string FullName { get; set; } = null!;

        public string? AvatarUrl { get; set; }

        public string? Email { get; set; }

        public ShopRole ShopRole { get; set; }

        public ShopMemberStatus Status { get; set; }

        public DateTime JoinedAt { get; set; }
    }
}