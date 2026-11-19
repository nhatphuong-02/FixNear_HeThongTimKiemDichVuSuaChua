namespace FixNear.Features.Shops.DTOs
{
    public class NearbyShopResponseDto
    {
        public int ShopId { get; set; }

        public string ShopName { get; set; } = null!;

        public string? Description { get; set; }

        public string? Phone { get; set; }

        public string? LogoUrl { get; set; }

        public decimal RatingAverage { get; set; }

        public string Address { get; set; } = null!;

        public double DistanceKm { get; set; }
    }
}