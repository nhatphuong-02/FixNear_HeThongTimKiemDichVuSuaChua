using FixNear.Enums;

namespace FixNear.Features.Shops.DTOs
{
    public class ShopProfileResponseDto
    {
        public int ShopId { get; set; }

        public int ManagerId { get; set; }

        public int AddressId { get; set; }

        public string ShopName { get; set; } = null!;

        public string? Description { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public string? LogoUrl { get; set; }

        public string? TaxCode { get; set; }

        public decimal RatingAverage { get; set; }

        public int Status { get; set; }

        public bool IsVerified { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public ShopRole CurrentUserRole { get; set; }
    }
}