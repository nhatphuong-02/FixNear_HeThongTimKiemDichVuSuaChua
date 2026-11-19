using FixNear.Features.ServiceCatalog.DTOs;

namespace FixNear.Features.ServiceCatalog.Interfaces
{
    public interface IServiceCategoryService
    {
        Task<List<ServiceCategoryResponseDto>> GetCategoriesAsync();
        Task<ServiceCategoryDetailDto> GetCategoryByIdAsync(int categoryId);
    }
}
