using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.AccountSecurity.DTOs;

public class RequestChangeEmailOtpDto
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string NewEmail { get; set; } = null!;
}