namespace FixNear.Features.Technicians.DTOs;
public sealed class TechnicianAssignmentDto
{
    public int RepairAssignmentId { get; set; }
    public int RepairRequestId { get; set; }
    public int ShopId { get; set; }
    public string ShopName { get; set; } = "";
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string? Description { get; set; }
    public int RepairMethod { get; set; }
    public DateOnly? PreferredDate { get; set; }
    public TimeOnly? PreferredTimeFrom { get; set; }
    public TimeOnly? PreferredTimeTo { get; set; }
    public int RepairStatus { get; set; }
    public int AssignmentStatus { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? Note { get; set; }
    public string? RejectionReason { get; set; }

    public List<AssignmentServiceDto> Services { get; set; } = [];
}

// Dịch vụ liên quan đến Repair
public sealed class AssignmentServiceDto
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = "";
}
