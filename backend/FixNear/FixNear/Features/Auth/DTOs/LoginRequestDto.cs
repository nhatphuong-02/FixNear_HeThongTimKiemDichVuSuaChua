using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Auth.DTOs
{
    public class LoginRequestDto
    {
        public string? Phone { get; set; }
        public string? Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
