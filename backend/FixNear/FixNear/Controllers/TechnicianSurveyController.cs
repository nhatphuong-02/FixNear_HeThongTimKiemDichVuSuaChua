
using FixNear.DTOs.Technician;
using FixNear.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FixNear.Controllers
{
    [ApiController]
    [Authorize(Roles = "3")]
    [Route("api/technician/surveys")]
    public class TechnicianSurveyController : ControllerBase
    {
        private readonly ITechnicianSurveyService _service;

        public TechnicianSurveyController(
            ITechnicianSurveyService service)
        {
            _service = service;
        }

        // ========================================
        // X03 - GHI NHAN KET QUA KHAO SAT
        // POST /api/technician/surveys
        // ========================================

        [HttpPost]
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
            var result = await _service.CreateSurveyAsync(
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
