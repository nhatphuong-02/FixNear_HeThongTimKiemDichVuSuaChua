using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Manager;

public class AcceptRepairRequestDto
{
    [StringLength(500)]
    public string? Note { get; set; }
}
