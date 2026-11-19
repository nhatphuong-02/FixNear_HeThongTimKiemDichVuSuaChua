using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Technicians.DTOs;

public sealed class CreateSurveyReportDto
{
    [Required(ErrorMessage = "Vui lòng nhập chẩn đoán.")]
    [StringLength(2000, MinimumLength = 5)]
    public string Diagnosis { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Recommendation { get; set; }
}
