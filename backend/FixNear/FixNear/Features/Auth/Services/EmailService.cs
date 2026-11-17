using FixNear.Features.Auth.Configurations;
using FixNear.Features.Auth.Interfaces;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FixNear.Features.Auth.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendVerificationCodeAsync(
        string toEmail,
        string otp,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_settings.SenderName,_settings.SenderEmail));

        message.To.Add(MailboxAddress.Parse(toEmail));

        message.Subject = "FixNear - Xác thực địa chỉ Email";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $"""
                <div style="font-family: Arial, sans-serif;
                            max-width: 500px; margin: auto;">
                    <h2>Chào mừng bạn đến với FixNear!</h2>

                    <p>
                        Đây là mã xác thực email của bạn:
                    </p>

                    <h1 style="letter-spacing: 8px;
                               color: #2563eb;">
                        {otp}
                    </h1>

                    <p>
                        Mã xác thực có hiệu lực trong 5 phút.
                    </p>

                    <p>
                        Nếu bạn không thực hiện yêu cầu này,
                        vui lòng bỏ qua email.
                    </p>
                </div>
                """,

            TextBody = $"""
                Chào mừng bạn đến với FixNear!

                Mã xác thực email của bạn: {otp}

                Mã có hiệu lực trong 5 phút.
                Nếu bạn không thực hiện yêu cầu này,
                vui lòng bỏ qua email.
                """
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(_settings.SmtpServer,_settings.Port,SecureSocketOptions.StartTls,cancellationToken);

            await client.AuthenticateAsync(_settings.Username,_settings.Password,cancellationToken);

            await client.SendAsync(message, cancellationToken);
        }
        finally
        {
            if (client.IsConnected)
                await client.DisconnectAsync(true, cancellationToken);
        }
    }

}
