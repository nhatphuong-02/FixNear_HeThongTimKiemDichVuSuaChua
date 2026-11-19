namespace FixNear.Features.Technicians.DTOs
{
    public class TechnicianScheduleDto
    {
        public int RepairScheduleId { get; set; }
        public int RepairRequestId { get; set; }
        public int ShopId { get; set; }
        public string ShopName { get; set; } = "";
        public string? Description { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public int Status { get; set; }
        public bool IsCurrent { get; set; }
    }
}
