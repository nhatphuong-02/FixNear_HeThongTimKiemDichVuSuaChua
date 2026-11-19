
using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Customers.DTOs;

public class CreateRepairRequestDto
{
    [Range(1, int.MaxValue)]
    public int ShopId { get; set; }

    [Range(1, int.MaxValue)]
    public int ServiceId { get; set; }

    // 1: Sửa tại nhà, 2: Sửa tại cửa hàng
    [Range(1, 2)]
    public int RepairMethod { get; set; }

    // Bắt buộc khi sửa tại nhà
    public int? AddressId { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateOnly? PreferredDate { get; set; }

    [Required]
    public TimeOnly? PreferredTimeFrom { get; set; }

    [Required]
    public TimeOnly? PreferredTimeTo { get; set; }
}
