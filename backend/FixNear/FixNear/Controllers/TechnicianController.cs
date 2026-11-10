using System.Security.Claims;
using FixNear.DTOs.Technician;
using FixNear.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixNear.Controllers
{
    [ApiController]
    [Authorize(Roles = "3")]
    [Route("api/technician")]
    public class TechnicianController : ControllerBase
    {




        private readonly ITechnicianService _technicianService;

        public TechnicianController(
            ITechnicianService technicianService)
        {
            _technicianService = technicianService;
        }





        // ============================================
        // X10 - GHI NHAN LINH KIEN SU DUNG
        // POST /api/repair-part-usages
        // ============================================

        [HttpPost("/api/repair-part-usages")]
        public async Task<IActionResult> RecordPartUsage(
            [FromBody] CreateRepairPartUsageDto dto)
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

            // 2. Goi TechnicianService
            var result = await _technicianService
                .RecordPartUsageAsync(dto, userId);

            // 3. Tra ve ket qua HTTP
            return result switch
            {
                RecordPartUsageResult.Success =>
                    StatusCode(201, new
                    {
                        message = "Ghi nhận linh kiện sử dụng thành công"
                    }),

                RecordPartUsageResult.TechnicianNotFound =>
                    NotFound(new
                    {
                        message = "Không tìm thấy thông tin kỹ thuật viên"
                    }),

                RecordPartUsageResult.AssignmentNotAccepted =>
                    StatusCode(403, new
                    {
                        message = "Kỹ thuật viên chưa nhận công việc hoặc không được phân công"
                    }),

                RecordPartUsageResult.RepairPartNotFound =>
                    NotFound(new
                    {
                        message = "Không tìm thấy linh kiện hoặc linh kiện đã ngừng hoạt động"
                    }),

                RecordPartUsageResult.QuoteNotAccepted =>
                    Conflict(new
                    {
                        message = "Chưa có báo giá được khách hàng chấp nhận"
                    }),

                RecordPartUsageResult.PartNotInQuote =>
                    Conflict(new
                    {
                        message = "Linh kiện không nằm trong báo giá đã được chấp nhận"
                    }),

                RecordPartUsageResult.InvalidQuantity =>
                    BadRequest(new
                    {
                        message = "Thông tin yêu cầu, linh kiện hoặc số lượng không hợp lệ"
                    }),

                RecordPartUsageResult.QuantityExceeded =>
                    Conflict(new
                    {
                        message = "Số lượng sử dụng vượt quá số lượng đã báo giá"
                    }),

                RecordPartUsageResult.InvalidQuotePrice =>
                    Conflict(new
                    {
                        message = "Đơn giá linh kiện trong báo giá không hợp lệ hoặc không thống nhất"
                    }),

                _ => StatusCode(500, new
                {
                    message = "Lỗi xử lý ghi nhận linh kiện"
                })
            };
        }

        // ============================================
        // X11 - HOAN THANH SUA CHUA
        // POST /api/repair-reports/{id}/complete
        // ============================================

        [HttpPost("/api/repair-reports/{id:int}/complete")]
        public async Task<IActionResult> CompleteRepair(
            int id,
            [FromBody] CompleteRepairDto dto)
        {
            var userIdText = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdText, out int userId))
            {
                return Unauthorized(new
                {
                    message = "Tài khoản không hợp lệ"
                });
            }

            var result = await _technicianService
                .CompleteRepairAsync(id, userId, dto);

            return result switch
            {
                CompleteRepairResult.Success =>
                    Ok(new
                    {
                        message = "Hoàn thành sửa chữa thành công"
                    }),

                CompleteRepairResult.ReportNotFound =>
                    NotFound(new
                    {
                        message = "Không tìm thấy báo cáo sửa chữa"
                    }),

                CompleteRepairResult.NotAssigned =>
                    StatusCode(403, new
                    {
                        message = "Kỹ thuật viên không có quyền hoàn thành công việc này"
                    }),

                CompleteRepairResult.QuoteNotAccepted =>
                    Conflict(new
                    {
                        message = "Khách hàng chưa chấp nhận báo giá"
                    }),

                CompleteRepairResult.InvalidStatus =>
                    Conflict(new
                    {
                        message = "Yêu cầu không ở trạng thái đang sửa chữa"
                    }),

                CompleteRepairResult.AlreadyCompleted =>
                    Conflict(new
                    {
                        message = "Yêu cầu đã chuyển sang chờ nghiệm thu"
                    }),

                _ => StatusCode(500)
            };
        }

        // ============================================
        // X01 - KY THUAT VIEN NHAN CONG VIEC
        // PATCH /api/technician/assignments/{id}/accept
        // ============================================

        [HttpPatch("assignments/{id:int}/accept")]
        public async Task<IActionResult> Accept(int id)
        {
            var userIdText = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdText, out int userId))
                return Unauthorized();

            var result = await _technicianService
     .AcceptAsync(id, userId);

            return result switch
            {
                AcceptAssignmentResult.Success =>
                    Ok(new
                    {
                        message = "Nhận việc thành công"
                    }),

                AcceptAssignmentResult.NotFound =>
                    NotFound(new
                    {
                        message = "Không tìm thấy công việc được giao"
                    }),

                AcceptAssignmentResult.InvalidStatus =>
                    Conflict(new
                    {
                        message = "Công việc không ở trạng thái chờ nhận"
                    }),

                _ => StatusCode(500)
            };
        }

        // ============================================
        // X02 - KY THUAT VIEN TU CHOI CONG VIEC
        // PATCH /api/technician/assignments/{id}/reject
        // ============================================

        [HttpPatch("assignments/{id:int}/reject")]
        public async Task<IActionResult> Reject(
            int id,
            [FromBody] RejectAssignmentDto request)
        {
            var userIdText = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdText, out int userId))
            {
                return Unauthorized(new
                {
                    message = "Tai khoan khong hop le"
                });
            }

            if (string.IsNullOrWhiteSpace(request.Reason) ||
                request.Reason.Trim().Length > 500)
            {
                return BadRequest(new
                {
                    message = "Ly do tu choi phai tu 1 den 500 ky tu"
                });
            }

            var result = await _technicianService.RejectAsync(
            id,
            userId,
            request.Reason);

            return result switch
            {
                RejectAssignmentResult.Success =>
                    Ok(new
                    {
                        message = "Từ chối công việc thành công"
                    }),

                RejectAssignmentResult.NotFound =>
                    NotFound(new
                    {
                        message = "Không tìm thấy công việc được giao"
                    }),

                RejectAssignmentResult.InvalidStatus =>
                    Conflict(new
                    {
                        message = "Công việc không ở trạng thái chờ phản hồi"
                    }),

                RejectAssignmentResult.InvalidReason =>
                    BadRequest(new
                    {
                        message = "Lý do từ chối không hợp lệ"
                    }),

                _ => StatusCode(500)
            };
        }


        // ============================================
        // X03 - GHI NHAN KET QUA KHAO SAT
        // POST /api/technician/surveys
        // ============================================

        [HttpPost("surveys")]
        public async Task<IActionResult> CreateSurvey(
            [FromBody] CreateSurveyReportDto request)
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

            // 2. Goi Service xu ly khao sat

            var result = await _technicianService.CreateSurveyAsync(
                userId,
                request.RepairRequestId,
                request.Diagnosis,
                request.Recommendation);

            // 3. Tra ve ket qua HTTP
            return result switch
            {
                SurveyResult.Success =>
                    Ok(new
                    {
                        message = "Ghi nhận kết quả khảo sát thành công"
                    }),

                SurveyResult.NotFound =>
                    NotFound(new
                    {
                        message = "Không tìm thấy công việc được giao"
                    }),

                SurveyResult.AssignmentNotAccepted =>
                    Conflict(new
                    {
                        message = "Kỹ thuật viên chưa nhận công việc"
                    }),

                SurveyResult.AlreadyExists =>
                    Conflict(new
                    {
                        message = "Báo cáo khảo sát đã tồn tại"
                    }),

                SurveyResult.InvalidData =>
                    BadRequest(new
                    {
                        message = "Thông tin khảo sát không hợp lệ"
                    }),

                _ => StatusCode(500)
            };
        }


    }
}
