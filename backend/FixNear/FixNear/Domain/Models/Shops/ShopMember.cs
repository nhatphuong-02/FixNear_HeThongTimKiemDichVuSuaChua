using FixNear.Domain.Models.Managers;
using FixNear.Enums;

namespace FixNear.Domain.Models.Shops
{
    public partial class ShopMember
    {
        public int ShopMemberId { get; set; }

        public int ShopId { get; set; }

        public int ManagerId { get; set; }

        public ShopRole ShopRole { get; set; }

        public DateTime JoinedAt { get; set; }

        public DateTime? LeftAt { get; set; }

        public ShopMemberStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual Shop Shop { get; set; } = null!;

        public virtual ManagerProfile Manager { get; set; } = null!;
    }
}