
using System.Security.Claims;
using FixNear.DTOs.Invoice;
using FixNear.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixNear.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/invoices")]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        // ============================================
        // X12 - TAO HOA DON
        // POST /api/invoices
        // ============================================

        [HttpPost]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> CreateInvoice(
            [FromBody] CreateInvoiceDto dto)
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

            // 2. Goi InvoiceService
            var result = await _invoiceService.CreateAsync(
                dto, userId);

            // 3. Tra ket qua HTTP
            return result.Status switch
            {
                CreateInvoiceStatus.Success =>
                    StatusCode(201, new
                    {
                        message = "Tạo hóa đơn thành công",
                        data = new
                        {
                            invoiceId = result.InvoiceId,
                            invoiceNumber = result.InvoiceNumber,
                            totalAmount = result.TotalAmount
                        }
                    }),

                CreateInvoiceStatus.InvalidInput =>
                    BadRequest(new
                    {
                        message = "Dữ liệu tạo hóa đơn không hợp lệ"
                    }),

                CreateInvoiceStatus.NotFound =>
                    NotFound(new
                    {
                        message = "Không tìm thấy yêu cầu sửa chữa hoặc không có quyền tạo hóa đơn"
                    }),

                CreateInvoiceStatus.InvalidRequestStatus =>
                    Conflict(new
                    {
                        message = "Yêu cầu sửa chữa chưa đủ điều kiện lập hóa đơn"
                    }),

                CreateInvoiceStatus.QuoteNotAccepted =>
                    Conflict(new
                    {
                        message = "Báo giá chưa được khách hàng chấp nhận"
                    }),

                CreateInvoiceStatus.EmptyQuote =>
                    Conflict(new
                    {
                        message = "Báo giá không có chi tiết"
                    }),

                CreateInvoiceStatus.InvalidQuoteDetail =>
                    Conflict(new
                    {
                        message = "Chi tiết báo giá không hợp lệ"
                    }),

                CreateInvoiceStatus.AlreadyExists =>
                    Conflict(new
                    {
                        message = "Yêu cầu sửa chữa đã có hóa đơn"
                    }),

                _ => StatusCode(500, new
                {
                    message = "Lỗi xử lý tạo hóa đơn"
                })
            };
        }

        // ============================================
        // X13 - XEM CHI TIET HOA DON
        // GET /api/invoices/{id}
        // ============================================

        [HttpGet("{id:int}")]
        [Authorize(Roles = "1,2")]
        public async Task<IActionResult> GetInvoice(int id)
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

            // 2. Lay vai tro tu JWT
            var roleText = User.FindFirstValue(
                ClaimTypes.Role);

            if (!int.TryParse(roleText, out int roleType))
            {
                return StatusCode(403, new
                {
                    message = "Vai trò tài khoản không hợp lệ"
                });
            }

            // 3. Goi InvoiceService
            var result = await _invoiceService.GetByIdAsync(
                id,
                userId,
                roleType);

            // 4. Tra ket qua HTTP
            return result.Status switch
            {
                GetInvoiceStatus.Success =>
                    Ok(new
                    {
                        message = "Lấy thông tin hóa đơn thành công",
                        data = result.Data
                    }),

                GetInvoiceStatus.NotFound =>
                    NotFound(new
                    {
                        message = "Không tìm thấy hóa đơn hoặc không có quyền xem"
                    }),

                _ => StatusCode(500, new
                {
                    message = "Lỗi xử lý xem hóa đơn"
                })
            };
        }

    }
}
