using FixNear.Enums;
using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Customer
{
    public class UpdateProfileRequestDto
    {
        [Required]
        [MinLength(3)]
        [MaxLength(200)]
        public string FullName { get; set; } = null;
        [Phone]
        public string? Phone { get; set; }
        [EmailAddress]
        public string? Email { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public Gender? Gender { get; set; }
        [Url]
        public string? AvatarUrl { get; set; }
    }
}
