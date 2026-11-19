using FixNear.Features.Technicians.DTOs;

public class TechnicianProfileResponseDto
{
    public int TechnicianId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? AvatarUrl { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public bool? Gender { get; set; }

    public int? ExperienceYears { get; set; }

    public string? Introduction { get; set; }

    public decimal RatingAverage { get; set; }

    public List<TechnicianSpecializationResponseDto> Specializations { get; set; } = new();
}