namespace FixNear.Features.ServiceCatalog.DTOs
{
    public class ServiceResponseDto
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = null!;
        public string? Description { get; set; }
    }
}
