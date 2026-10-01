using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Auth;

public class RegisterRequestDto
{
    [Required]
    public string FullName { get; set; } = null!;
    [EmailAddress]
    public string? Email { get; set; }
    public string? Phone { get; set; }

    [MinLength(8)]
    public string Password { get; set; } = null!;
}