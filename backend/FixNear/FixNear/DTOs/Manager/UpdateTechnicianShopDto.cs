using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Manager;

public class UpdateTechnicianShopDto
{
    [Range(1, int.MaxValue)]
    public int Status { get; set; } = 1;

    public bool IsPrimary { get; set; }
}
