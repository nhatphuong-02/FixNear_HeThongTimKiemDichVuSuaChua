
namespace FixNear.Features.Technicians.DTOs;

public class TechnicianShopSummaryDto
{
    public int TechnicianShopId { get; set; }
    public int ShopId { get; set; }
    public string ShopName { get; set; } = "";
    public string? LogoUrl { get; set; }
    public string? Phone { get; set; }
    public int Status { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime JoinedAt { get; set; }
}


