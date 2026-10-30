
using FixNear.DTOs.Quote;

namespace FixNear.Services.Interfaces;

public interface IQuoteService
{
    // X04 - Ky thuat vien tao bao gia
    Task<CreateQuoteResult> CreateAsync(
        int userId,
        CreateQuoteDto request);
}

public enum CreateQuoteStatus
{
    Success,
    NotFound,
    AssignmentNotAccepted,
    SurveyNotFound,
    InvalidData,
    InvalidItem
}

public class CreateQuoteResult
{
    public CreateQuoteStatus Status { get; set; }

    public int? QuoteId { get; set; }

    public decimal? TotalAmount { get; set; }
}
