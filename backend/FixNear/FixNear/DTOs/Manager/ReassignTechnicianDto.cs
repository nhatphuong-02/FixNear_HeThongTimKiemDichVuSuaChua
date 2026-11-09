using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Manager;

public class ReassignTechnicianDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Mã kỹ thuật viên phải lớn hơn 0.")]
    public int TechnicianId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do đổi kỹ thuật viên.")]
    [StringLength(500, MinimumLength = 1,
        ErrorMessage = "Lý do phải có từ 1 đến 500 ký tự.")]
    public string Reason { get; set; } = string.Empty;
}
