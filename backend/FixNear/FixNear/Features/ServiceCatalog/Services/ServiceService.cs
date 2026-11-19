using FixNear.Features.ServiceCatalog.DTOs;
using FixNear.Features.ServiceCatalog.Interfaces;
using FixNear.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.ServiceCatalog.Services
{
    public class ServiceService : IServiceService
    {
        private readonly FixNearDbContext _context;

        public ServiceService(FixNearDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách dịch vụ đang hoạt động
        public async Task<List<ServiceResponseDto>> GetServicesAsync()
        {
            return await _context.Service
                .AsNoTracking()
                .Where(s => s.IsActive && s.Category.IsActive)
                .OrderBy(s => s.ServiceName)
                .Select(s => new ServiceResponseDto
                {
                    ServiceId = s.ServiceId,
                    CategoryId = s.CategoryId,
                    CategoryName = s.Category.CategoryName,
                    ServiceName = s.ServiceName,
                    Description = s.Description
                })
                .ToListAsync();
        }

        // Lấy chi tiết một dịch vụ
        public async Task<ServiceDetailDto> GetServiceByIdAsync(int serviceId)
        {
            var service = await _context.Service
                .AsNoTracking()
                .Where(s =>
                    s.ServiceId == serviceId &&
                    s.IsActive &&
                    s.Category.IsActive)
                .Select(s => new ServiceDetailDto
                {
                    ServiceId = s.ServiceId,
                    ServiceName = s.ServiceName,
                    Description = s.Description,
                    CategoryId = s.CategoryId,
                    CategoryName = s.Category.CategoryName
                })
                .FirstOrDefaultAsync();

            if (service == null)
                throw new KeyNotFoundException("Không tìm thấy dịch vụ");

            return service;
        }
    }
}