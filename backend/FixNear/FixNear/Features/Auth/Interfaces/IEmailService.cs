namespace FixNear.Features.Auth.Interfaces
{
    public interface IEmailService
    {
        Task SendVerificationCodeAsync(string toEmail, string otp, CancellationToken cancellationToken = default);
        Task SendTechnicianInvitationAsync(string toEmail, string shopName, string invitationUrl, DateTime expiresAt, CancellationToken cancellationToken = default);
    }
}
