namespace FixNear.Features.Auth.Interfaces
{
    public interface IEmailService
    {
        Task SendVerificationCodeAsync(string toEmail, string otp, CancellationToken cancellationToken = default);
    }
}
