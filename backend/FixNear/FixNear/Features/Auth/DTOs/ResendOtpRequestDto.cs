using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Auth.DTOs;

public class ResendOtpRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = null!;
}