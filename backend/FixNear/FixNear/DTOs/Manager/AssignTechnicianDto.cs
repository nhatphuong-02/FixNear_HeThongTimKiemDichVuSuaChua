using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Manager;

public class AssignTechnicianDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Mã yêu cầu phải lớn hơn 0.")]
    public int RepairRequestId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Mã kỹ thuật viên phải lớn hơn 0.")]
    public int TechnicianId { get; set; }

    [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
    public string? Note { get; set; }
}
