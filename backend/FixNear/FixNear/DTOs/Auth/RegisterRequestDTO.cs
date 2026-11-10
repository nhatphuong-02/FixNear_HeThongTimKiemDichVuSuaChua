using FixNear.Enums;
using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Auth;

public class RegisterRequestDto
{
    [Required]
    public string FullName { get; set; } = null!;
    [EmailAddress]
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }

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

    public required string RecipientName { get; set; }
    
    public required string RecipientPhone { get; set; }

}