using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Auth.DTOs
{
    public class VerifyEmailRequestDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(6, MinimumLength = 6)]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã xác thực phải là 6 chữ số.")]
        public string Otp { get; set; } = null!;
    }
}
