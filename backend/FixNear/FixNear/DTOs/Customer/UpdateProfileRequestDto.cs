
using FixNear.Enums;
using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Customer
{
    public class UpdateProfileRequestDto
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [MinLength(3, ErrorMessage = "Họ tên phải có ít nhất 3 ký tự")]
        [MaxLength(200)]
        public string FullName { get; set; } = null!;

        public DateOnly? DateOfBirth { get; set; }

        [EnumDataType(typeof(Gender))]
        public Gender? Gender { get; set; }

        [MaxLength(500)]
        [Url]
        public string? AvatarUrl { get; set; }
    }
}
