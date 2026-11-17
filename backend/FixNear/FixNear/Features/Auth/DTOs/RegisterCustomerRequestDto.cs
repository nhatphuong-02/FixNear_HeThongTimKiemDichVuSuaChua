using FixNear.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Auth.DTOs;

public class RegisterCustomerRequestDto : RegisterAccountRequestDto
{

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