
using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Review
{
    public class CreateReviewDto
    {
        [Range(1, int.MaxValue,
            ErrorMessage = "Mã yêu cầu sửa chữa không hợp lệ")]
        public int RepairRequestId { get; set; }

        [Range(1, 5,
            ErrorMessage = "Số sao đánh giá phải từ 1 đến 5")]
        public int Rating { get; set; }

        [MaxLength(2000,
            ErrorMessage = "Bình luận không được vượt quá 2000 ký tự")]
        public string? Comment { get; set; }

        public int? TechnicianId { get; set; }

        public List<string> ImageUrls { get; set; } = new();
    }
}
