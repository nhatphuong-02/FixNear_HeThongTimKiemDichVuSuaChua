
using FixNear.DTOs.Review;

namespace FixNear.Services.Interfaces
{
    public interface IReviewService
    {
        // X16 - Tao danh gia
        Task<CreateReviewResult> CreateAsync(
            CreateReviewDto dto,
            int userId);
        // X17 - Xem danh sach danh gia cua Shop
        Task<GetShopReviewsResult> GetByShopAsync(
            int shopId,
            int page,
            int pageSize);
        // X18 - Manager phan hoi danh gia
        Task<CreateReviewReplyResult> ReplyAsync(
            int reviewId,
            int managerUserId,
            CreateReviewReplyDto dto);
    }

    public enum CreateReviewStatus
    {
        Success,
        InvalidInput,
        RequestNotFound,
        NotCompleted,
        NotPaid,
        AlreadyReviewed,
        TechnicianNotAssigned,
        InvalidImages
    }

    public class CreateReviewResult
    {
        public CreateReviewStatus Status { get; set; }

        public int? ReviewId { get; set; }

        public int? RepairRequestId { get; set; }

        public int? Rating { get; set; }

        public int? ImageCount { get; set; }
    }

    public enum GetShopReviewsStatus
    {
        Success,
        InvalidInput,
        ShopNotFound
    }

    public class GetShopReviewsResult
    {
        public GetShopReviewsStatus Status { get; set; }

        public ShopReviewsResponseDto? Data { get; set; }
    }

    public enum CreateReviewReplyStatus
    {
        Success,
        InvalidInput,
        ReviewNotFound,
        AlreadyReplied
    }

    public class CreateReviewReplyResult
    {
        public CreateReviewReplyStatus Status { get; set; }

        public int? ReplyId { get; set; }

        public int? ReviewId { get; set; }

        public string? ReplyContent { get; set; }
    }

}
