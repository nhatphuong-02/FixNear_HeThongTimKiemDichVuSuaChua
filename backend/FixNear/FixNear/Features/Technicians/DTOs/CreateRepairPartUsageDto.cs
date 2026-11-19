using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Technicians.DTOs;

public class CreateRepairPartUsageDto
{
    [Range(1, int.MaxValue)]
    public int RepairRequestId { get; set; }

    [Range(1, int.MaxValue)]
    public int RepairPartId { get; set; }

    [Range(typeof(decimal), "0.01", "999999")]
    public decimal Quantity { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}
