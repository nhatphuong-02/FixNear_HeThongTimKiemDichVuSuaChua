
using FixNear.DTOs.Quote;

namespace FixNear.Services.Interfaces;

public interface IQuoteService
{
    // X04 - Ky thuat vien tao bao gia
    Task<CreateQuoteResult> CreateAsync(
        int userId,
        CreateQuoteDto request);
    // X05 - Xem chi tiet bao gia
    Task<QuoteResponseDto?> GetByIdAsync(
        int quoteId,
        int userId,
        int roleType);
    // X06 - Manager gui bao gia cho khach hang
    Task<SendQuoteResult> SendAsync(
        int quoteId,
        int userId);
    // X07 - Customer chap nhan bao gia
    Task<AcceptQuoteResult> AcceptQuoteAsync(
        int quoteId,
        int userId);
    // X08 - Customer tu choi bao gia
    Task<RejectQuoteResult> RejectQuoteAsync(
        int quoteId,
        int userId);
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

public enum SendQuoteResult
{
    Success,
    NotFound,
    InvalidStatus
}

public enum AcceptQuoteResult
{
    Success,
    NotFound,
    InvalidStatus,
    Expired
}

public enum RejectQuoteResult
{
    Success,
    NotFound,
    InvalidStatus,
    Expired
}



public class CreateQuoteResult
{
    public CreateQuoteStatus Status { get; set; }

    public int? QuoteId { get; set; }

    public decimal? TotalAmount { get; set; }
}
