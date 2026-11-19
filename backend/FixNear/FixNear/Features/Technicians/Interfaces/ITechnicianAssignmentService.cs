using FixNear.Features.Technicians.DTOs;

namespace FixNear.Features.Technicians.Interfaces;

public interface ITechnicianAssignmentService
{
    // Xem toàn bộ công việc của KTV
    Task<List<TechnicianAssignmentDto>> GetMyAssignmentsAsync(
        int userId);

    // Xem chi tiết công việc
    Task<TechnicianAssignmentDto?> GetAssignmentDetailAsync(
        int userId,
        int assignmentId);

    // Chấp nhận công việc
    Task AcceptAssignmentAsync(
        int userId,
        int assignmentId);

    // Từ chối công việc
    Task RejectAssignmentAsync(
        int userId,
        int assignmentId,
        string reason);
}
