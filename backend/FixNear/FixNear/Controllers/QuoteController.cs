
using FixNear.DTOs.Quote;
using FixNear.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FixNear.Controllers
{
    [ApiController]
    [Authorize]
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
        [Authorize(Roles = "3")]
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

        // ============================================
        // X05 - XEM CHI TIET BAO GIA
        // GET /api/quotes/{id}
        // ============================================
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetQuote(int id)
        {
            // 1. Lay thong tin dang nhap tu JWT
            var userIdText = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var roleText = User.FindFirstValue(
                ClaimTypes.Role);

            if (!int.TryParse(userIdText, out int userId) ||
                !int.TryParse(roleText, out int roleType))
            {
                return Unauthorized(new
                {
                    message = "Tài khoản không hợp lệ"
                });
            }

            // 2. Goi Service lay bao gia
            var quote = await _quoteService.GetByIdAsync(
                id,
                userId,
                roleType);

            if (quote == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy báo giá hoặc không có quyền xem"
                });
            }

            // 3. Tra thong tin bao gia
            return Ok(new
            {
                message = "Lấy thông tin báo giá thành công",
                data = quote
            });
        }

        // ============================================
        // X06 - MANAGER GUI BAO GIA CHO KHACH HANG
        // PATCH /api/quotes/{id}/send
        // ============================================

        [Authorize(Roles = "2")]
        [HttpPatch("{id:int}/send")]
        public async Task<IActionResult> SendQuote(int id)
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

            // 2. Goi Service xu ly gui bao gia
            var result = await _quoteService.SendAsync(
                id,
                userId);

            // 3. Tra ket qua HTTP
            return result switch
            {
                SendQuoteResult.Success =>
                    Ok(new
                    {
                        message = "Gửi báo giá thành công"
                    }),

                SendQuoteResult.NotFound =>
                    NotFound(new
                    {
                        message = "Không tìm thấy báo giá hoặc không có quyền gửi"
                    }),

                SendQuoteResult.InvalidStatus =>
                    Conflict(new
                    {
                        message = "Báo giá không ở trạng thái nháp"
                    }),

                _ => StatusCode(500)
            };
        }

        // ============================================
        // X07 - CUSTOMER CHAP NHAN BAO GIA
        // PATCH /api/quotes/{id}/accept
        // ============================================

        [Authorize(Roles = "1")]
        [HttpPatch("{id:int}/accept")]
        public async Task<IActionResult> AcceptQuote(int id)
        {
            // 1. Lay UserId cua Customer tu JWT
            var userIdText = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdText, out int userId))
            {
                return Unauthorized(new
                {
                    message = "Tài khoản không hợp lệ"
                });
            }

            // 2. Goi Service xu ly chap nhan bao gia
            var result = await _quoteService.AcceptQuoteAsync(
                id,
                userId);

            // 3. Tra ket qua HTTP
            return result switch
            {
                AcceptQuoteResult.Success =>
                    Ok(new
                    {
                        message = "Chấp nhận báo giá thành công"
                    }),

                AcceptQuoteResult.NotFound =>
                    NotFound(new
                    {
                        message = "Không tìm thấy báo giá hoặc không có quyền chấp nhận"
                    }),

                AcceptQuoteResult.InvalidStatus =>
                    Conflict(new
                    {
                        message = "Báo giá chưa được gửi hoặc đã có phản hồi"
                    }),

                AcceptQuoteResult.Expired =>
                    Conflict(new
                    {
                        message = "Báo giá đã hết hạn"
                    }),

                _ => StatusCode(500)
            };
        }

        // ============================================
        // X08 - CUSTOMER TU CHOI BAO GIA
        // PATCH /api/quotes/{id}/reject
        // ============================================

        [Authorize(Roles = "1")]
        [HttpPatch("{id:int}/reject")]
        public async Task<IActionResult> RejectQuote(int id)
        {
            // 1. Lay UserId cua Customer tu JWT
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
            var result = await _quoteService.RejectQuoteAsync(
                id,
                userId);

            // 3. Tra ket qua HTTP
            return result switch
            {
                RejectQuoteResult.Success =>
                    Ok(new
                    {
                        message = "Từ chối báo giá thành công"
                    }),

                RejectQuoteResult.NotFound =>
                    NotFound(new
                    {
                        message = "Không tìm thấy báo giá hoặc không có quyền từ chối"
                    }),

                RejectQuoteResult.InvalidStatus =>
                    Conflict(new
                    {
                        message = "Báo giá chưa được gửi hoặc đã có phản hồi"
                    }),

                RejectQuoteResult.Expired =>
                    Conflict(new
                    {
                        message = "Báo giá đã hết hạn"
                    }),

                _ => StatusCode(500)
            };
        }




    }
}
