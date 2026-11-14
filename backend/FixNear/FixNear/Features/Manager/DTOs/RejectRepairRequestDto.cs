using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Manager.DTOs;

public class RejectRepairRequestDto
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Reason { get; set; } = string.Empty;
}
