using FixNear.DTOs.Service;
using FixNear.Features.ServiceCatalog.DTOs;
using FixNear.Features.ServiceCatalog.Interfaces;
using FixNear.Infrastructor.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.ServiceCatalog.Services
{
    public class ServiceCategoryService : IServiceCategoryService
    {
        private readonly FixNearDbContext _context;
        public ServiceCategoryService(FixNearDbContext context)
        {
            _context = context;
        }

        //Lấy danh mục dịch vụ hoạt động
        public async Task<List<ServiceCategoryResponeDto>> GetCategoriesAsync()
        {
            return await _context.ServiceCategory
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategoryName)
                .Select(c => new ServiceCategoryResponeDto
                {
                    ServiceCategoryId = c.ServiceCategoryId,
                    CategoryName = c.CategoryName,
                    Description = c.Description
                }).ToListAsync();
        }

        //Lấy chi tiết danh mục dịch vụ theo ID
        public async Task<ServiceCategoryDetailDto> GetCategoryByIdAsync(int categoryId)
        {
            var category = await _context.ServiceCategory
                .AsNoTracking()
                .Where(c => c.ServiceCategoryId == categoryId && c.IsActive)
                .Select(c => new ServiceCategoryDetailDto
                {
                    ServiceCategoryId = c.ServiceCategoryId,
                    CategoryName = c.CategoryName,
                    Description = c.Description,

                    Services = c.Service
                        .Where(s => s.IsActive)
                        .OrderBy(s => s.ServiceName)
                        .Select(s => new ServiceResponseDto
                        {
                            ServiceId = s.ServiceId,
                            ServiceName = s.ServiceName,
                            Description = s.Description
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            if(category == null)
                throw new KeyNotFoundException("Không tìm thấy danh mục dịch vụ");

            return category;
        }
    }
}
