using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Manager;

public class RejectRepairRequestDto
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Reason { get; set; } = string.Empty;
}
