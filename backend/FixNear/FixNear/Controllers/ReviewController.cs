
using System.Security.Claims;
using FixNear.DTOs.Review;
using FixNear.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixNear.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/reviews")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        // ============================================
        // X16 - TAO DANH GIA
        // POST /api/reviews
        // ============================================
        [HttpPost]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> CreateReview(
            [FromBody] CreateReviewDto dto)
        {
            // 1. Lay UserId tu JWT
            var userIdText = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdText, out int userId))
            {
                return Unauthorized(new
                {
                    message = "Tài khoản không hợp lệ"
                });
            }

            // 2. Goi ReviewService
            var result = await _reviewService.CreateAsync(
                dto,
                userId);

            // 3. Tra ket qua HTTP
            return result.Status switch
            {
                CreateReviewStatus.Success =>
                    StatusCode(201, new
                    {
                        message = "Tạo đánh giá thành công",
                        data = new
                        {
                            reviewId = result.ReviewId,
                            repairRequestId = result.RepairRequestId,
                            rating = result.Rating,
                            imageCount = result.ImageCount
                        }
                    }),

                CreateReviewStatus.InvalidInput =>
                    BadRequest(new
                    {
                        message = "Thông tin đánh giá không hợp lệ"
                    }),

                CreateReviewStatus.RequestNotFound =>
                    NotFound(new
                    {
                        message = "Không tìm thấy yêu cầu sửa chữa hoặc không có quyền đánh giá"
                    }),

                CreateReviewStatus.NotCompleted =>
                    Conflict(new
                    {
                        message = "Yêu cầu sửa chữa chưa hoàn tất"
                    }),

                CreateReviewStatus.NotPaid =>
                    Conflict(new
                    {
                        message = "Yêu cầu sửa chữa chưa được xác nhận thanh toán"
                    }),

                CreateReviewStatus.AlreadyReviewed =>
                    Conflict(new
                    {
                        message = "Yêu cầu sửa chữa đã được đánh giá"
                    }),

                CreateReviewStatus.TechnicianNotAssigned =>
                    Conflict(new
                    {
                        message = "Kỹ thuật viên không thuộc yêu cầu sửa chữa này"
                    }),

                CreateReviewStatus.InvalidImages =>
                    BadRequest(new
                    {
                        message = "Danh sách ảnh đánh giá không hợp lệ"
                    }),

                _ => StatusCode(500, new
                {
                    message = "Lỗi xử lý tạo đánh giá"
                })
            };
        }

        // ============================================
        // X17 - XEM DANH GIA CUA SHOP
        // GET /api/shops/{shopId}/reviews
        // ============================================

        [HttpGet("/api/shops/{shopId:int}/reviews")]
        [AllowAnonymous]
        public async Task<IActionResult> GetShopReviews(
            int shopId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // 1. Goi ReviewService
            var result = await _reviewService.GetByShopAsync(
                shopId,
                page,
                pageSize);

            // 2. Tra ket qua HTTP
            return result.Status switch
            {
                GetShopReviewsStatus.Success =>
                    Ok(new
                    {
                        message = "Lấy danh sách đánh giá thành công",
                        data = result.Data
                    }),

                GetShopReviewsStatus.InvalidInput =>
                    BadRequest(new
                    {
                        message = "Thông tin phân trang hoặc mã Shop không hợp lệ"
                    }),

                GetShopReviewsStatus.ShopNotFound =>
                    NotFound(new
                    {
                        message = "Không tìm thấy cửa hàng"
                    }),

                _ => StatusCode(500, new
                {
                    message = "Lỗi xử lý danh sách đánh giá"
                })
            };
        }

        // ============================================
        // X18 - PHAN HOI DANH GIA
        // POST /api/reviews/{id}/reply
        // ============================================

        [HttpPost("{id:int}/reply")]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> ReplyToReview(
            int id,
            [FromBody] CreateReviewReplyDto dto)
        {
            // 1. Lay UserId cua Manager tu JWT
            var userIdText = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdText, out int userId))
            {
                return Unauthorized(new
                {
                    message = "Tài khoản không hợp lệ"
                });
            }

            // 2. Goi ReviewService
            var result = await _reviewService.ReplyAsync(
                id,
                userId,
                dto);

            // 3. Tra ket qua HTTP
            return result.Status switch
            {
                CreateReviewReplyStatus.Success =>
                    StatusCode(201, new
                    {
                        message = "Phản hồi đánh giá thành công",
                        data = new
                        {
                            replyId = result.ReplyId,
                            reviewId = result.ReviewId,
                            replyContent = result.ReplyContent
                        }
                    }),

                CreateReviewReplyStatus.InvalidInput =>
                    BadRequest(new
                    {
                        message = "Nội dung phản hồi không hợp lệ"
                    }),

                CreateReviewReplyStatus.ReviewNotFound =>
                    NotFound(new
                    {
                        message = "Không tìm thấy đánh giá hoặc không có quyền phản hồi"
                    }),

                CreateReviewReplyStatus.AlreadyReplied =>
                    Conflict(new
                    {
                        message = "Đánh giá này đã được phản hồi"
                    }),

                _ => StatusCode(500, new
                {
                    message = "Lỗi xử lý phản hồi đánh giá"
                })
            };
        }
  
    }
}
