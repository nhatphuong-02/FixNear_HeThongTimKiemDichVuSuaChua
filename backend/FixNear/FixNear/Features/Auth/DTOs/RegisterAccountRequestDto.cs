using FixNear.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Auth.DTOs;

public class RegisterAccountRequestDto
{
    [Required]
    [MaxLength(255)]
    public string FullName { get; set; } = null!;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public Gender? Gender { get; set; }

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = null!;
}