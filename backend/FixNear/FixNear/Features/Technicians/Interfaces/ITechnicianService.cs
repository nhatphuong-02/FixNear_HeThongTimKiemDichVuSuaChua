
using FixNear.Features.Technicians.DTOs;

namespace FixNear.Features.Technicians.Interfaces;

public interface ITechnicianService
{
    Task<TechnicianProfileResponseDto?>
        GetProfileAsync(int userId);

    Task<bool> UpdateProfileAsync(
        int userId,
        UpdateTechnicianProfileRequestDto request);

    Task<List<TechnicianShopSummaryDto>>
        GetMyShopsAsync(int userId);

    Task<List<TechnicianScheduleDto>>
        GetMySchedulesAsync(
            int userId,
            DateTime? from,
            DateTime? to);
}
