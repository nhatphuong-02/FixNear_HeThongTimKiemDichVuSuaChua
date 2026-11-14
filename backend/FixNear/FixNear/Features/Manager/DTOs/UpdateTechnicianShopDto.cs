using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Manager.DTOs;

public class UpdateTechnicianShopDto
{
    [Range(1, int.MaxValue)]
    public int Status { get; set; } = 1;

    public bool IsPrimary { get; set; }
}
