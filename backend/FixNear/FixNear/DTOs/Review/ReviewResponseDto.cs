
namespace FixNear.DTOs.Review
{
    public class ReviewResponseDto
    {
        public int ReviewId { get; set; }

        public int ShopId { get; set; }

        public int? TechnicianId { get; set; }

        public int Rating { get; set; }

        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<ReviewImageResponseDto> Images { get; set; }
            = new();
    }

    public class ReviewImageResponseDto
    {
        public int ReviewImageId { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }
    }

    public class ShopReviewsResponseDto
    {
        public int ShopId { get; set; }

        public int TotalCount { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }

        public List<ReviewResponseDto> Reviews { get; set; }
            = new();
    }
}
