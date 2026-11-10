
using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Review
{
    public class CreateReviewReplyDto
    {
        [Required(ErrorMessage = "Nội dung phản hồi không được để trống")]
        [StringLength(2000, MinimumLength = 1,
            ErrorMessage = "Nội dung phản hồi phải từ 1 đến 2000 ký tự")]
        public string ReplyContent { get; set; } = string.Empty;
    }
}
