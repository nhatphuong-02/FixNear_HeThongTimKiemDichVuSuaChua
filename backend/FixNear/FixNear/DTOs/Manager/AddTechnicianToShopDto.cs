using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Manager;

public class AddTechnicianToShopDto
{
    [Range(1, int.MaxValue)]
    public int TechnicianId { get; set; }

    [Range(1, int.MaxValue)]
    public int Status { get; set; } = 1;

    public bool IsPrimary { get; set; } = false;
}
