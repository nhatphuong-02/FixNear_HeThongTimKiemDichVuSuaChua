namespace FixNear.Features.Shops.DTOs;

public class ShopTechnicianDetailResponseDto
{
    public int TechnicianShopId { get; set; }

    public int TechnicianId { get; set; }

    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public string? Email { get; set; }

    public int? ExperienceYears { get; set; }

    public string? Introduction { get; set; }

    public int TechnicianStatus { get; set; }

    public decimal RatingAverage { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime JoinedAt { get; set; }

    public DateTime? LeftAt { get; set; }
}