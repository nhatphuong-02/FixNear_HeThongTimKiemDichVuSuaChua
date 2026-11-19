using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Shops.DTOs
{
    public class NearbyShopRequestDto
    {
        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Range(-180, 180)]
        public double Longitude { get; set; }

        [Range(1, 100)]
        public double RadiusKm { get; set; } = 10;
    }
}