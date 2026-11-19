namespace FixNear.Features.ServiceCatalog.DTOs
{
    public class ServiceDetailDto
    {
        public int ServiceId { get; set; }

        public string ServiceName { get; set; } = null!;

        public string? Description { get; set; }

        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = null!;
    }
}