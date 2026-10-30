
using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Technician;

public class RejectAssignmentDto
{
    [Required(ErrorMessage = "Vui lòng nhập lý do từ chối.")]
    [StringLength(500, ErrorMessage =
        "Lý do từ chối không được vượt quá 500 ký tự.")]
    public string Reason { get; set; } = string.Empty;
}
