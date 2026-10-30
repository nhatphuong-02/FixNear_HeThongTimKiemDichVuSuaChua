
namespace FixNear.Services.Interfaces;

public interface ITechnicianSurveyService
{
    Task<SurveyResult> CreateSurveyAsync(
        int userId,
        int repairRequestId,
        string diagnosis,
        string? recommendation);
}

public enum SurveyResult
{
    Success,
    NotFound,
    AssignmentNotAccepted,
    AlreadyExists,
    InvalidData
}
