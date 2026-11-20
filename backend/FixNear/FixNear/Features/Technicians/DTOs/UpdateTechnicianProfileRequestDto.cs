using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Technicians.DTOs;

public class UpdateTechnicianProfileRequestDto
{
    [Range(0, 60)]
    public int? ExperienceYears { get; set; }

    [MaxLength(2000)]
    public string? Introduction { get; set; }

    public List<int> ServiceIds { get; set; } = new();
}
