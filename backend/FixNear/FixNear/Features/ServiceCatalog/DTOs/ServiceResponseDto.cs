namespace FixNear.Features.ServiceCatalog.DTOs
{
    public class ServiceResponseDto
    {
        public int ServiceId { get; set; }

        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = null!;

        public string ServiceName { get; set; } = null!;

        public string? Description { get; set; }
    }
}