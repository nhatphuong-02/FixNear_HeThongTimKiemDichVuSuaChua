
namespace FixNear.DTOs.Customer
{
    public class AddressResponseDto
    {
        public int AddressId { get; set; }

        public int ProvinceId { get; set; }
        public string ProvinceName { get; set; } = null!;

        public int WardId { get; set; }
        public string WardName { get; set; } = null!;

        public string AddressLine { get; set; } = null!;

        public string? RecipientName { get; set; }
        public string? RecipientPhone { get; set; }

        public bool IsDefault { get; set; }
        public bool IsLocked { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
