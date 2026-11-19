using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Auth.DTOs;

public class RegisterTechnicianRequestDto : RegisterAccountRequestDto
{
    [Range(0, 60, ErrorMessage = "Số năm kinh nghiệm không hợp lệ")]
    public int? ExperienceYears { get; set; }

    [MaxLength(1000)]
    public string? Introduction { get; set; }

    public List<int> ServiceIds { get; set; } = new();
}