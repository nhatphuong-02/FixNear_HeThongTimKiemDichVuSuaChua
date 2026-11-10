using System.Security.Claims;
using FixNear.DTOs.Payment;
using FixNear.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixNear.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/payments")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(
            IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // ============================================
        // X14 - TAO YEU CAU THANH TOAN
        // POST /api/payments
        // ============================================

        [HttpPost]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> CreatePayment(
            [FromBody] CreatePaymentDto dto)
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

            // 2. Goi PaymentService
            var result = await _paymentService.CreateAsync(
                dto,
                userId);

            // 3. Tra ket qua HTTP
            return result.Status switch
            {
                CreatePaymentStatus.Success =>
                    StatusCode(201, new
                    {
                        message = "Tạo yêu cầu thanh toán thành công",
                        data = new
                        {
                            paymentId = result.PaymentId,
                            invoiceId = result.InvoiceId,
                            amount = result.Amount,
                            paymentMethod = result.PaymentMethod,
                            status = result.PaymentStatus
                        }
                    }),

                CreatePaymentStatus.InvalidInput =>
                    BadRequest(new
                    {
                        message = "Thông tin thanh toán không hợp lệ"
                    }),

                CreatePaymentStatus.InvoiceNotFound =>
                    NotFound(new
                    {
                        message = "Không tìm thấy hóa đơn hoặc không có quyền thanh toán"
                    }),

                CreatePaymentStatus.InvalidAmount =>
                    Conflict(new
                    {
                        message = "Số tiền trên hóa đơn không hợp lệ"
                    }),

                CreatePaymentStatus.AlreadyPaid =>
                    Conflict(new
                    {
                        message = "Hóa đơn đã được thanh toán"
                    }),

                CreatePaymentStatus.PaymentAlreadyExists =>
                    Conflict(new
                    {
                        message = "Hóa đơn đã có yêu cầu thanh toán"
                    }),

                _ => StatusCode(500, new
                {
                    message = "Lỗi xử lý thanh toán"
                })
            };
        }

        // ============================================
        // X15 - XEM THONG TIN THANH TOAN
        // GET /api/payments/{id}
        // ============================================

        [HttpGet("{id:int}")]
        [Authorize(Roles = "1,2")]
        public async Task<IActionResult> GetPayment(int id)
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

            // 2. Lay RoleType tu JWT
            var roleText = User.FindFirstValue(
                ClaimTypes.Role);

            if (!int.TryParse(roleText, out int roleType))
            {
                return StatusCode(403, new
                {
                    message = "Vai trò tài khoản không hợp lệ"
                });
            }

            // 3. Goi PaymentService
            var result = await _paymentService.GetByIdAsync(
                id,
                userId,
                roleType);

            // 4. Tra ket qua HTTP
            return result.Status switch
            {
                GetPaymentStatus.Success =>
                    Ok(new
                    {
                        message = "Lấy thông tin thanh toán thành công",
                        data = result.Data
                    }),

                GetPaymentStatus.NotFound =>
                    NotFound(new
                    {
                        message = "Không tìm thấy thanh toán hoặc không có quyền xem"
                    }),

                _ => StatusCode(500, new
                {
                    message = "Lỗi xử lý xem thanh toán"
                })
            };
        }

    }
}
