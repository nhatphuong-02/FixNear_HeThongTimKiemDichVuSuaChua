using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Shops.DTOs;

public class InviteTechnicianRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = null!;
}