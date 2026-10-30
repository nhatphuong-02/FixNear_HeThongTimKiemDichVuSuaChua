
using FixNear.DTOs.Quote;
using FixNear.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FixNear.Controllers
{
    [ApiController]
    [Authorize(Roles = "3")]
    [Route("api/quotes")]
    public class QuoteController : ControllerBase
    {
        private readonly IQuoteService _quoteService;

        public QuoteController(IQuoteService quoteService)
        {
            _quoteService = quoteService;
        }

        // ====================================
        // X04 - KY THUAT VIEN TAO BAO GIA
        // POST /api/quotes
        // ====================================
        [HttpPost]
        public async Task<IActionResult> CreateQuote(
            [FromBody] CreateQuoteDto request)
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

            // 2. Goi QuoteService
            var result = await _quoteService.CreateAsync(
                userId,
                request);

            // 3. Tra ket qua HTTP
            return result.Status switch
            {
                CreateQuoteStatus.Success =>
                    Ok(new
                    {
                        message = "Tạo báo giá thành công",
                        quoteId = result.QuoteId,
                        totalAmount = result.TotalAmount
                    }),

                CreateQuoteStatus.NotFound =>
                    NotFound(new
                    {
                        message = "Không tìm thấy công việc được giao"
                    }),

                CreateQuoteStatus.AssignmentNotAccepted =>
                    Conflict(new
                    {
                        message = "Kỹ thuật viên chưa nhận công việc"
                    }),

                CreateQuoteStatus.SurveyNotFound =>
                    Conflict(new
                    {
                        message = "Chưa có báo cáo khảo sát"
                    }),

                CreateQuoteStatus.InvalidData =>
                    BadRequest(new
                    {
                        message = "Dữ liệu báo giá không hợp lệ"
                    }),

                CreateQuoteStatus.InvalidItem =>
                    BadRequest(new
                    {
                        message = "Hạng mục báo giá không hợp lệ"
                    }),

                _ => StatusCode(500)
            };
        }
    }
}
