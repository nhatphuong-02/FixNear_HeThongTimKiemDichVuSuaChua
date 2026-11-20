namespace FixNear.Features.Technicians.DTOs;

public class TechnicianSpecializationResponseDto
{
    public int ServiceId { get; set; }

    public string ServiceName { get; set; } = string.Empty;

    public string? Note { get; set; }
}
