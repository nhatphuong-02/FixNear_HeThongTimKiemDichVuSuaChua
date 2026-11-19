using FixNear.Features.ServiceCatalog.DTOs;

namespace FixNear.Features.ServiceCatalog.Interfaces
{
    public interface IServiceService
    {
        Task<List<ServiceResponseDto>> GetServicesAsync();

        Task<ServiceDetailDto> GetServiceByIdAsync(int serviceId);
    }
}