using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Technicians.DTOs;

public class CompleteRepairDto
{
    [Required]
    public string Result { get; set; } = string.Empty;

    public string? WorkDescription { get; set; }

    public string? Recommendation { get; set; }
}
