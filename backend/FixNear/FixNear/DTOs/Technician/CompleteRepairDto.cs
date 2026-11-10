
using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Technician
{
    public class CompleteRepairDto
    {
        [Required(ErrorMessage = "Kết quả sửa chữa không được để trống")]
        public string Result { get; set; } = string.Empty;

        public string? WorkDescription { get; set; }

        public string? Recommendation { get; set; }
    }
}
