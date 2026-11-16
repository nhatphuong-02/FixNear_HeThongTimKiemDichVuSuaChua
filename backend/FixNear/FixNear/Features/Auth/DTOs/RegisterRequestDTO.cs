using FixNear.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Auth.DTOs;

public class RegisterRequestDto
{
    [Required]
    public string FullName { get; set; } = null!;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = null!;

    // Thông tin địa chỉ
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn tỉnh/thành phố")]
    public int ProvinceId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn phường/xã")]
    public int WardId { get; set; }

    [Required]
    [MaxLength(500)]
    public string AddressLine { get; set; } = null!;

    [Required]
    public string RecipientName { get; set; } = null!;

    [Required]
    public string RecipientPhone { get; set; } = null!;

}