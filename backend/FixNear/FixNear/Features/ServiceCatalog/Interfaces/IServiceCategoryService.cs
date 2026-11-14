using FixNear.Features.ServiceCatalog.DTOs;

namespace FixNear.Features.ServiceCatalog.Interfaces
{
    public interface IServiceCategoryService
    {
        Task<List<ServiceCategoryResponeDto>> GetCategoriesAsync();
        Task<ServiceCategoryDetailDto> GetCategoryByIdAsync(int categoryId);
    }
}
