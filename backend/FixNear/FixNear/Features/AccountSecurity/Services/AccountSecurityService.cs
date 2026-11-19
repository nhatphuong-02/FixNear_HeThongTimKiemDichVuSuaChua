using FixNear.Domain.Enums;
using FixNear.Domain.Models.Identity;
using FixNear.Features.AccountSecurity.DTOs;
using FixNear.Features.AccountSecurity.Interfaces;
using FixNear.Features.Auth.Interfaces;
using FixNear.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Cryptography;

namespace FixNear.Features.AccountSecurity.Services;

public class AccountSecurityService : IAccountSecurityService
{
    private readonly FixNearDbContext _context;
    private readonly IEmailService _emailService;

    private const int OtpExpiryMinutes = 5;
    private const int OtpResendCooldownSeconds = 60;
    private const int MaxOtpAttempts = 5;

    public AccountSecurityService( FixNearDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    //Đổi mật khẩu
    public async Task RequestChangePasswordOtpAsync(int userId)
    {
        var user = await GetUserAsync(userId);

        if (string.IsNullOrWhiteSpace(user.Email))
            throw new InvalidOperationException("Tài khoản chưa có địa chỉ email.");

        await CreateAndSendOtpAsync(user,user.Email,EmailVerificationPurpose.ChangePassword);
    }

    public async Task ChangePasswordAsync(
        int userId,
        ChangePasswordRequestDto request)
    {
        await ExecuteOtpProtectedChangeAsync(userId, EmailVerificationPurpose.ChangePassword, null, request.Otp, async user =>
            {
                var passwordHasher = new PasswordHasher<User>();

                var passwordResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);

                if (passwordResult == PasswordVerificationResult.Failed)
                    throw new InvalidOperationException("Mật khẩu hiện tại không chính xác.");

                var newPasswordResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.NewPassword);

                if (newPasswordResult != PasswordVerificationResult.Failed)
                    throw new InvalidOperationException("Mật khẩu mới phải khác mật khẩu hiện tại.");

                user.PasswordHash = passwordHasher.HashPassword( user, request.NewPassword);

                await Task.CompletedTask;
            });
    }

    //Đổi email
    public async Task RequestChangeEmailOtpAsync(int userId, RequestChangeEmailOtpDto request)
    {
        var user = await GetUserAsync(userId);

        var newEmail = NormalizeEmail(request.NewEmail);

        if (string.Equals(user.Email?.Trim(), newEmail, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Email mới phải khác email hiện tại.");

        var emailExists = await _context.User.AnyAsync(u =>
            u.Email != null &&
            u.Email.ToLower() == newEmail &&
            u.UserId != userId);

        if (emailExists)
            throw new InvalidOperationException("Email đã được sử dụng bởi tài khoản khác.");

        await CreateAndSendOtpAsync(user, newEmail, EmailVerificationPurpose.ChangeEmail);
    }

    public async Task ChangeEmailAsync(int userId, ChangeEmailRequestDto request)
    {
        var newEmail = NormalizeEmail(request.NewEmail);

        await ExecuteOtpProtectedChangeAsync(userId, EmailVerificationPurpose.ChangeEmail, newEmail,request.Otp, async user =>
            {
                var emailExists = await _context.User.AnyAsync(u =>
                    u.Email != null &&
                    u.Email.ToLower() == newEmail &&
                    u.UserId != userId);

                if (emailExists)
                    throw new InvalidOperationException("Email đã được sử dụng bởi tài khoản khác.");

                if (string.Equals(
                    user.Email,
                    newEmail,
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Email mới phải khác email hiện tại.");
                }

                user.Email = newEmail;

                await Task.CompletedTask;
            });
    }

    //Đổi số điện thoại
    public async Task RequestChangePhoneOtpAsync(int userId)
    {
        var user = await GetUserAsync(userId);

        if (string.IsNullOrWhiteSpace(user.Email))
            throw new InvalidOperationException("Tài khoản chưa có địa chỉ email.");

        await CreateAndSendOtpAsync(user, user.Email, EmailVerificationPurpose.ChangePhone);
    }

    public async Task ChangePhoneAsync(int userId, ChangePhoneRequestDto request)
    {
        var newPhone = request.NewPhone.Trim();

        await ExecuteOtpProtectedChangeAsync(userId,EmailVerificationPurpose.ChangePhone,null,request.Otp,async user =>
            {
                var phoneExists = await _context.User.AnyAsync(u =>
                    u.Phone == newPhone &&
                    u.UserId != userId);

                if (phoneExists)
                    throw new InvalidOperationException("Số điện thoại đã được sử dụng bởi tài khoản khác.");

                if (user.Phone == newPhone)
                    throw new InvalidOperationException("Số điện thoại mới phải khác số hiện tại.");

                user.Phone = newPhone;

                await Task.CompletedTask;
            });
    }

 
    //Hàm dùng chung
    private async Task<User> GetUserAsync(int userId)
    {
        var user = await _context.User
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
            throw new KeyNotFoundException("Không tìm thấy tài khoản.");

        return user;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    // Tạo và gửi OTP
    private async Task CreateAndSendOtpAsync(User user, string email, EmailVerificationPurpose purpose)
    {
        var now = DateTime.UtcNow;

        var oldVerifications = await _context.EmailVerifications
            .Where(e =>
                e.UserId == user.UserId &&
                e.Purpose == purpose &&
                !e.IsUsed)
            .ToListAsync();

        var latestVerification = oldVerifications
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefault();

        if (latestVerification != null && latestVerification.CreatedAt.AddSeconds(OtpResendCooldownSeconds) > now)
            throw new InvalidOperationException("Vui lòng chờ 60 giây trước khi yêu cầu gửi lại OTP.");

        // Vô hiệu hóa các OTP cũ
        foreach (var verification in oldVerifications)
            verification.IsUsed = true;

        // Tạo OTP mới
        var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        var newVerification = new EmailVerification
        {
            UserId = user.UserId,
            Email = NormalizeEmail(email),
            Purpose = purpose,
            RegistrationType = null,
            RegistrationData = null,
            ExpiresAt = now.AddMinutes(OtpExpiryMinutes),
            CreatedAt = now,
            FailedAttempts = 0,
            IsUsed = false
        };

        var otpHasher = new PasswordHasher<EmailVerification>();

        newVerification.OtpHash = otpHasher.HashPassword(newVerification, otp);

        _context.EmailVerifications.Add(newVerification);

        await _context.SaveChangesAsync();

        try
        {
            await _emailService.SendVerificationCodeAsync(email, otp);
        }
        catch
        {
            // Nếu gửi email thất bại, vô hiệu hóa OTP vừa tạo
            newVerification.IsUsed = true;
            await _context.SaveChangesAsync();

            throw;
        }
    }

    // Xác thực OTP và thực hiện thay đổi tài khoản
    private async Task ExecuteOtpProtectedChangeAsync(int userId, EmailVerificationPurpose purpose, string? expectedEmail, string otp, Func<User, Task> updateAction)
    {
        // Serializable giúp hạn chế hai yêu cầu đồng thời cùng tiêu thụ một OTP.
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var user = await _context.User.FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
            throw new KeyNotFoundException("Không tìm thấy tài khoản.");

        var verification = await _context.EmailVerifications
            .Where(e =>
                e.UserId == userId &&
                e.Purpose == purpose &&
                !e.IsUsed)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync();

        if (verification == null)
        {
            await transaction.CommitAsync();

            throw new InvalidOperationException("Không tìm thấy yêu cầu OTP hợp lệ.");
        }

        var requiredEmail = NormalizeEmail( expectedEmail ?? user.Email ?? string.Empty);

        if (string.IsNullOrWhiteSpace(requiredEmail) ||!string.Equals(verification.Email, requiredEmail, StringComparison.OrdinalIgnoreCase))
        {
            await transaction.CommitAsync();

            throw new InvalidOperationException("Email xác thực không khớp với yêu cầu.");
        }

        if (verification.ExpiresAt <= DateTime.UtcNow ||
            verification.FailedAttempts >= MaxOtpAttempts)
        {
            verification.IsUsed = true;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            throw new InvalidOperationException("OTP đã hết hạn hoặc vượt quá số lần nhập sai.");
        }

        var otpHasher = new PasswordHasher<EmailVerification>();

        var otpResult = otpHasher.VerifyHashedPassword(
            verification,
            verification.OtpHash,
            otp);

        if (otpResult == PasswordVerificationResult.Failed)
        {
            verification.FailedAttempts++;

            if (verification.FailedAttempts >= MaxOtpAttempts)
                verification.IsUsed = true;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            throw new InvalidOperationException(
                verification.IsUsed
                    ? "Bạn đã nhập sai OTP quá số lần cho phép. Vui lòng yêu cầu mã mới."
                    : $"OTP không chính xác. Bạn còn {MaxOtpAttempts - verification.FailedAttempts} lần thử.");
        }

        // OTP hợp lệ: thực hiện thay đổi tài khoản
        await updateAction(user);

        verification.VerifiedAt = DateTime.UtcNow;
        verification.IsUsed = true;

        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await transaction.CommitAsync();
    }
}