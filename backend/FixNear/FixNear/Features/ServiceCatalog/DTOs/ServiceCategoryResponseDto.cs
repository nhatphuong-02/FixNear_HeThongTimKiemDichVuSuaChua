namespace FixNear.Features.ServiceCatalog.DTOs
{
    public class ServiceCategoryResponseDto
    {
        public int ServiceCategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string? Description { get; set; } = null!;
    }
}
