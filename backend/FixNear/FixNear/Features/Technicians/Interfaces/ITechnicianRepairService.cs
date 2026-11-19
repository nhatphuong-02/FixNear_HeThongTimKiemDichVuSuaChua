
using FixNear.Features.Technicians.DTOs;

namespace FixNear.Features.Technicians.Interfaces;

public interface ITechnicianRepairService
{
    // Bắt đầu khảo sát
    Task StartSurveyAsync(
        int userId,
        int repairRequestId);

    // Lưu báo cáo khảo sát
    Task<int> CompleteSurveyAsync(
        int userId,
        int repairRequestId,
        CreateSurveyReportDto dto);

    // Bắt đầu sửa chữa sau khi Customer duyệt báo giá
    Task StartRepairAsync(
        int userId,
        int repairRequestId);

    // Ghi nhận linh kiện thực tế đã sử dụng
    Task<int> RecordPartUsageAsync(
        int userId,
        int repairRequestId,
        CreateRepairPartUsageDto dto);

    // Hoàn thành sửa chữa
    Task CompleteRepairAsync(
        int userId,
        int repairRequestId,
        CompleteRepairDto dto);
}
