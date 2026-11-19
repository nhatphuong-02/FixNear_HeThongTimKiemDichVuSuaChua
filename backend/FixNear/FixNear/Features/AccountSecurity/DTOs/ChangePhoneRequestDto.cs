using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.AccountSecurity.DTOs;

public class ChangePhoneRequestDto
{
    [Required]
    [Phone]
    [MaxLength(20)]
    public string NewPhone { get; set; } = null!;

    [Required]
    [RegularExpression(@"^\d{6}$")]
    public string Otp { get; set; } = null!;
}