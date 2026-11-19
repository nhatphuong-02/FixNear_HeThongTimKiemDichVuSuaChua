
namespace FixNear.Features.Repairs.DTOs;

public sealed class UpcomingScheduleDto
{
    public int RepairScheduleId { get; set; }

    public int RepairRequestId { get; set; }

    public int ShopId { get; set; }

    public string ShopName { get; set; } = "";

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = "";

    public int TechnicianId { get; set; }

    public string TechnicianName { get; set; } = "";

    public string? Description { get; set; }

    public int RepairMethod { get; set; }

    public int RepairStatus { get; set; }

    public int ScheduleStatus { get; set; }

    // Giá trị thời gian UTC
    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public List<string> ServiceNames { get; set; } = new();
}
