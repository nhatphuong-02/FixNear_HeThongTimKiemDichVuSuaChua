using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Manager.DTOs;

public class AcceptRepairRequestDto
{
    [StringLength(500)]
    public string? Note { get; set; }
}
