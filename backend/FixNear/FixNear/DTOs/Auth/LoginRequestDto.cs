using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Auth
{
    public class LoginRequestDto
    {
        public string? Phone { get; set; }
        public string? Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
