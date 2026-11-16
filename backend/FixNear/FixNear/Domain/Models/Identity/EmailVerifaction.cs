using FixNear.Domain.Enums;

namespace FixNear.Domain.Models.Identity;

public class EmailVerification
{
    public int EmailVerificationId { get; set; }

    public string Email { get; set; } = null!;

    public string OtpHash { get; set; } = null!;

    public int? UserId { get; set; }

    public EmailVerificationPurpose Purpose { get; set; }

    public string RegistrationData { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public int FailedAttempts { get; set; } = 0;

    public DateTime CreatedAt { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public bool IsUsed { get; set; } = false;

    public User? User { get; set; }
}
