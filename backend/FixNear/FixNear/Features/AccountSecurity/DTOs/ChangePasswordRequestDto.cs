using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.AccountSecurity.DTOs;

public class ChangePasswordRequestDto
{
    [Required]
    public string CurrentPassword { get; set; } = null!;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = null!;

    [Required]
    [RegularExpression(@"^\d{6}$")]
    public string Otp { get; set; } = null!;
}