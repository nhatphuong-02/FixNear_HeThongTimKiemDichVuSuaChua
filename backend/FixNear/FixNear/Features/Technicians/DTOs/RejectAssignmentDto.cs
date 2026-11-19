using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Technicians.DTOs;

public class RejectAssignmentDto
{
    [Required(ErrorMessage = "Vui lòng nhập lý do từ chối.")]
    [StringLength(500, MinimumLength = 1)]
    public string Reason { get; set; } = string.Empty;
}
