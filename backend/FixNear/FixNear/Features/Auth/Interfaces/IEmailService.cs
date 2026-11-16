namespace FixNear.Features.Auth.Interfaces
{
    public interface IEmailService
    {
        Task SendVertificationCodeAsync(string toEmail, string otp, CancellationToken cancellationToken = default);
    }
}
