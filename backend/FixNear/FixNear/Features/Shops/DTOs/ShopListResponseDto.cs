using FixNear.Enums;

namespace FixNear.Features.Shops.DTOs;

public class ShopListResponseDto
{
    public int ShopId { get; set; }
    public string ShopName { get; set; } = null!;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsVerified { get; set; }

    public ShopRole CurrentUserRole { get; set; }
    public DateTime JoinedAt { get; set; }
}