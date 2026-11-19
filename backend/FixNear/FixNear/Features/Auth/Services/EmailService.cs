using FixNear.Features.Auth.Configurations;
using FixNear.Features.Auth.Interfaces;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net;

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

    //
    public async Task SendTechnicianInvitationAsync(string toEmail, string shopName, string invitationUrl, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        var safeShopName = WebUtility.HtmlEncode(shopName);
        var safeInvitationUrl = WebUtility.HtmlEncode(invitationUrl);

        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));

        message.To.Add(MailboxAddress.Parse(toEmail));

        message.Subject = $"FixNear - Lời mời hợp tác từ {shopName}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $"""
            <div style="font-family: Arial, sans-serif;
                        max-width: 550px; margin: auto;">
                <h2>Bạn nhận được lời mời hợp tác!</h2>

                <p>
                    Shop <strong>{safeShopName}</strong>
                    muốn mời bạn tham gia đội ngũ kỹ thuật viên.
                </p>

                <p>
                    Nhấn vào nút bên dưới để xem lời mời:
                </p>

                <a href="{safeInvitationUrl}"
                   style="display: inline-block;
                          padding: 12px 24px;
                          background-color: #2563eb;
                          color: white;
                          text-decoration: none;
                          border-radius: 6px;">
                    Xem lời mời
                </a>

                <p>
                    Lời mời có hiệu lực đến:
                    {expiresAt.ToUniversalTime():dd/MM/yyyy HH:mm} UTC.
                </p>

                <p>
                    Nếu bạn không mong đợi lời mời này,
                    vui lòng bỏ qua email.
                </p>
            </div>
            """,

            TextBody = $"""
            Bạn nhận được lời mời hợp tác từ Shop {shopName} trên FixNear.

            Truy cập liên kết sau để xem lời mời:
            {invitationUrl}

            Lời mời có hiệu lực đến:
            {expiresAt.ToUniversalTime():dd/MM/yyyy HH:mm} UTC.

            Nếu bạn không mong đợi lời mời này, vui lòng bỏ qua email.
            """
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(
                _settings.SmtpServer,
                _settings.Port,
                SecureSocketOptions.StartTls,
                cancellationToken);

            await client.AuthenticateAsync(
                _settings.Username,
                _settings.Password,
                cancellationToken);

            await client.SendAsync(message, cancellationToken);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, CancellationToken.None);
            }
        }
    }
}
