using FixNear.Features.Technicians.DTOs;

namespace FixNear.Features.Technicians.Interfaces
{
    public interface ITechnicianService
    {
        Task<TechnicianProfileResponseDto> GetMyProfileAsync(int userId);

        Task UpdateMyProfileAsync(int userId, UpdateTechnicianProfileRequestDto request);
        Task AcceptTechnicianInvitationAsync(int userId, int invitationId);

        Task RejectTechnicianInvitationAsync(int userId, int invitationId);

        Task<List<TechnicianInvitationResponseDto>> GetMyInvitationsAsync(int userId);

        // X10 - Ghi nhan linh kien su dung
        Task<RecordPartUsageResult> RecordPartUsageAsync(
            CreateRepairPartUsageDto dto,
            int userId);
        // X11 - Hoan thanh sua chua
        Task<CompleteRepairResult> CompleteRepairAsync(
            int repairReportId,
            int userId,
            CompleteRepairDto dto);

        // X01 - KTV nhan cong viec
        Task<AcceptAssignmentResult> AcceptAsync(
            int assignmentId,
            int userId);

        // X02 - KTV tu choi cong viec
        Task<RejectAssignmentResult> RejectAsync(
            int assignmentId,
            int userId,
            string reason);
        // X03 - Ghi nhan ket qua khao sat
        Task<SurveyResult> CreateSurveyAsync(
            int userId,
            int repairRequestId,
            string diagnosis,
            string? recommendation);

    }

    public enum RecordPartUsageResult
    {
        Success,
        TechnicianNotFound,
        AssignmentNotAccepted,
        RepairPartNotFound,
        QuoteNotAccepted,
        PartNotInQuote,
        InvalidQuantity,
        QuantityExceeded,
        InvalidQuotePrice
    }

    public enum CompleteRepairResult
    {
        Success,
        ReportNotFound,
        NotAssigned,
        QuoteNotAccepted,
        InvalidStatus,
        AlreadyCompleted
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

    public enum SurveyResult
    {
        Success,
        NotFound,
        AssignmentNotAccepted,
        AlreadyExists,
        InvalidData
    }


}
