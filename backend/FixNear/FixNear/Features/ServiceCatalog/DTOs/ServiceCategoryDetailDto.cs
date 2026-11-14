namespace FixNear.Features.ServiceCatalog.DTOs
{
    public class ServiceCategoryDetailDto
    {
        public int ServiceCategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string? Description { get; set; }

        public List<ServiceResponseDto> Services { get; set; } = new List<ServiceResponseDto>();
    }
}
