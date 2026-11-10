
using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Technician
{
    public class CreateSurveyReportDto
    {
        [Range(1, int.MaxValue,
            ErrorMessage = "Mã yêu cầu sửa chữa không hợp lệ")]
        public int RepairRequestId { get; set; }

        [Required(ErrorMessage =
            "Vui lòng nhập kết quả chẩn đoán")]
        public string Diagnosis { get; set; } = string.Empty;

        public string? Recommendation { get; set; }
    }
}
