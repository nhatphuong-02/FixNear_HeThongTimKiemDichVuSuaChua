using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.AccountSecurity.DTOs;

public class ChangeEmailRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string NewEmail { get; set; } = null!;

    [Required]
    [RegularExpression(@"^\d{6}$")]
    public string Otp { get; set; } = null!;
}