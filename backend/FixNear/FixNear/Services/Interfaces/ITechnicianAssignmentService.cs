
namespace FixNear.Services.Interfaces
{
    public interface ITechnicianAssignmentService
    {
        // X01 - Nhan viec
        Task<AcceptAssignmentResult> AcceptAsync(
            int assignmentId,
            int userId);

        // X02 - Tu choi viec
        Task<RejectAssignmentResult> RejectAsync(
            int assignmentId,
            int userId,
            string reason);
    }

    public enum AcceptAssignmentResult
    {
        Success,
        NotFound,
        InvalidStatus
    }

    public enum RejectAssignmentResult
    {
        Success,
        NotFound,
        InvalidStatus,
        InvalidReason
    }
}
