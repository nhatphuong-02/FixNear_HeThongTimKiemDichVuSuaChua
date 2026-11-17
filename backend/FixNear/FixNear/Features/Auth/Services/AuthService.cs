using FixNear.Domain.Enums;
using FixNear.Domain.Models.Customers;
using FixNear.Domain.Models.Identity;
using FixNear.Features.Auth.DTOs;
using FixNear.Features.Auth.Interfaces;
using FixNear.Infrastructure.Persistence;
using FixNear.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Cryptography;
using System.Text.Json;

namespace FixNear.Features.Auth.Services
{
    public class AuthService : IAuthService
    {
        private readonly FixNearDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;

        public AuthService(FixNearDbContext context, IJwtService jwtService, IEmailService emailService)
        {
            _context = context;
            _jwtService = jwtService;
            _emailService = emailService;
        }
        private class RegistrationData
        {
            public string FullName { get; set; } = null!;
            public string Email { get; set; } = null!;
            public string? Phone { get; set; }
            public DateOnly? DateOfBirth { get; set; }
            public bool? Gender { get; set; }
            public string PasswordHash { get; set; } = null!;

            public int ProvinceId { get; set; }
            public int WardId { get; set; }
            public string AddressLine { get; set; } = null!;
            public string RecipientName { get; set; } = null!;
            public string RecipientPhone { get; set; } = null!;
        }

        public async Task RegisterAsync(RegisterRequestDto request)
        {
            // Kiểm tra dữ liệu đầu vào
            var email = request.Email?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(email))
                throw new InvalidOperationException("Email không hợp lệ");

            var phone = string.IsNullOrWhiteSpace(request.Phone) ? null: request.Phone.Trim();

            //Kiểm tra tài khoản đã tồn tại
            var existingUser = await _context.User
                .FirstOrDefaultAsync(u =>
                        (request.Email != null && u.Email == request.Email) ||
                        (request.Phone != null && u.Phone == request.Phone));

            if (existingUser != null)
                throw new InvalidOperationException("Người dùng đã tồn tại");

            // Kiểm tra phường/xã thuộc tỉnh/tp đã chọn
            var validWard = await _context.Ward.AnyAsync(w=>
               w.WardId == request.WardId && 
               w.ProvinceId == request.ProvinceId);

            if (!validWard)
                throw new InvalidOperationException("Phường/Xã không thuộc Tỉnh/Thành phố đã chọn");

            var now = DateTime.UtcNow;
            //Tìm yêu cầu đăng ký Otp gần nhất
            var oldVerifications = await _context.EmailVerifications
                .Where(e =>
                    e.Email == email &&
                    e.Purpose == EmailVerificationPurpose.Register &&
                    !e.IsUsed)
                .ToListAsync();

            //Kiểm tra thời gian chờ 60s
            var lastestVerification = oldVerifications.OrderByDescending(e => e.CreatedAt).FirstOrDefault();

            if (lastestVerification != null && lastestVerification.CreatedAt.AddSeconds(60) > now)
                throw new InvalidOperationException($"Vui lòng chờ 60 giây trước khi yêu cầu gửi lại OTP");

            //Hash mật khẩu
            var passwordHasher = new PasswordHasher<User>();
            var temporaryUser = new User();

            var passwordHash = passwordHasher.HashPassword(temporaryUser, request.Password);

            //Lưu thông tin đăng ký dưới dạng Json - Chuẩn bị dữ liệu đăng ký
            var registrationData = new RegistrationData
            {
                FullName = request.FullName,
                Email = email,
                Phone = phone,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender.HasValue
                    ? request.Gender.Value == Gender.Male
                    : null,
                PasswordHash = passwordHash,
                ProvinceId = request.ProvinceId,
                WardId = request.WardId,
                AddressLine = request.AddressLine,
                RecipientName = request.RecipientName,
                RecipientPhone = request.RecipientPhone
            };

            var registrationJson = JsonSerializer.Serialize(registrationData);

           //Vô hiệu hóa OTP cũ
            foreach (var verification in oldVerifications)
                verification.IsUsed = true;

            //Tạo OTP mới
            var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

            //Tạo bản ghi xác thực email
            var vertification = new EmailVerification
            {
                Email = email,
                Purpose = EmailVerificationPurpose.Register,
                RegistrationData = JsonSerializer.Serialize(registrationData),
                ExpiresAt = now.AddMinutes(5),
                CreatedAt = now,
                FailedAttempts = 0,
                IsUsed = false
            };

            //Hash OTP
            var otpHasher = new PasswordHasher<EmailVerification>();

            vertification.OtpHash = otpHasher.HashPassword(vertification, otp);

            _context.EmailVerifications.Add(vertification);
            await _context.SaveChangesAsync();

            //Gửi email sau khi lưu yêu cầu đăng ký
            await _emailService.SendVerificationCodeAsync(email,otp);
        }

        public async Task VerifyEmailAsync(VerifyEmailRequestDto request)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var now = DateTime.UtcNow;

            //Mở transaction để bảo vệ quá trình xác thực
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            //Tìm yêu cầu xác thực email gần nhất
            var verification = await _context.EmailVerifications
                .Where(e =>
                    e.Email == email &&
                    e.Purpose == EmailVerificationPurpose.Register &&
                    !e.IsUsed)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync();

            if (verification == null)
                throw new InvalidOperationException("Không tìm thấy yêu cầu xác thực hợp lệ");

            //Kiểm tra mã OTP hết hạn
            if (verification.ExpiresAt <= DateTime.UtcNow)
            {
                verification.IsUsed = true;
                await _context.SaveChangesAsync();
                throw new InvalidOperationException("Mã OTP đã hết hạn");
            }

            //Kiểm tra số lần nhập sai OTP
            if (verification.FailedAttempts >= 5)
            {
                verification.IsUsed = true;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                throw new InvalidOperationException("Bạn đã nhập sai OTP quá số lần cho phép");
            }

            //Kiểm tra mã OTP
            var otpHasher = new PasswordHasher<EmailVerification>();

            var otpResult = otpHasher.VerifyHashedPassword(verification, verification.OtpHash, request.Otp);

            if (otpResult == PasswordVerificationResult.Failed)
            {
                verification.FailedAttempts++;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                throw new InvalidOperationException("Mã OTP không chính xác");
            }

            //Lấy thông tin đăng ký đã lưu tạm
            var registrationData = JsonSerializer.Deserialize<RegistrationData>(verification.RegistrationData!);

            if (registrationData == null)
            {
                verification.IsUsed = true;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                throw new InvalidOperationException("Dữ liệu đăng ký không hợp lệ");
            }

            //Kiểm tra lại email và số điện thoại trước khi tạo người dùng mới
            var existingUser = await _context.User.AnyAsync(u =>
                    u.Email == email ||
                    (registrationData.Phone != null && u.Phone == registrationData.Phone));

            if (existingUser)
            {
                verification.IsUsed = true;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                throw new InvalidOperationException("Email hoặc số điện thoại đã được sử dụng");
            }

            //Tạo user
            var user = new User
            {
                FullName = registrationData.FullName,
                Email = registrationData.Email,
                Phone = registrationData.Phone,
                DateOfBirth = registrationData.DateOfBirth,
                Gender = registrationData.Gender,
                PasswordHash = registrationData.PasswordHash,
                RoleType = (int)RoleType.Customer,
                Status = (int)UserStatus.Active
            };

            _context.User.Add(user);
            await _context.SaveChangesAsync();

            // Tạo CustomerProfile
            var customerProfile = new CustomerProfile
            {
                UserId = user.UserId,
                CreatedAt = now
            };

            _context.CustomerProfile.Add(customerProfile);
            await _context.SaveChangesAsync();

            // Tạo địa chỉ cho Customer
            var address = new Address
            {
                OwnerType = (int)RoleType.Customer,
                CustomerId = customerProfile.CustomerId,
                ProvinceId = registrationData.ProvinceId,
                WardId = registrationData.WardId,
                AddressLine = registrationData.AddressLine,
                RecipientName = registrationData.RecipientName,
                RecipientPhone = registrationData.RecipientPhone,
                IsLocked = false,
                CreatedAt = now
            };

            _context.Address.Add(address);
            await _context.SaveChangesAsync();

            // Đặt địa chỉ vừa tạo làm mặc định
            customerProfile.DefaultAddressId = address.AddressId;

            // Đánh dấu email đã xác thực và OTP đã sử dụng
            verification.VerifiedAt = now;
            verification.IsUsed = true;

            await _context.SaveChangesAsync();

            // Hoàn tất transaction
            await transaction.CommitAsync();
        }

        public async Task ResendOtpAsync(ResendOtpRequestDto request)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var now = DateTime.UtcNow;

            //Tìm yêu cầu đăng ký Otp chưa sử dụng gần nhất
            var verification = await _context.EmailVerifications.Where(e =>
                e.Email == email &&
                e.Purpose == EmailVerificationPurpose.Register &&
                !e.IsUsed).OrderBy(e => e.CreatedAt).FirstOrDefaultAsync();

            if (verification == null)
                throw new InvalidOperationException("Không tìm thấy yêu cầu đăng ký xác thực");

            //Kiểm tra thời gian chờ 60s
            if (verification.CreatedAt.AddSeconds(60) > now)
                throw new InvalidOperationException("Vui lòng chờ 60 giây trước khi gửi lại OTP");

            //Kiểm tra dữ liệu đăng ký tạm
            if (string.IsNullOrWhiteSpace(verification.RegistrationData))
                throw new InvalidOperationException("Không tìm thấy dữ liệu đăng ký");

            // Vô hiệu hóa OTP cũ
            verification.IsUsed = true;

            // Tạo OTP mới
            var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

            var newVerification = new EmailVerification
            {
                Email = email,
                Purpose = EmailVerificationPurpose.Register,
                RegistrationData = verification.RegistrationData,
                ExpiresAt = now.AddMinutes(5),
                FailedAttempts = 0,
                CreatedAt = now,
                IsUsed = false
            };

            // Hash OTP mới
            var otpHasher = new PasswordHasher<EmailVerification>();

            newVerification.OtpHash = otpHasher.HashPassword(newVerification,otp);

            // Lưu OTP mới và vô hiệu hóa OTP cũ
            _context.EmailVerifications.Add(newVerification);

            await _context.SaveChangesAsync();

            // Gửi OTP mới qua email
            await _emailService.SendVerificationCodeAsync(email, otp);
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            var existingUser = await _context.User
                .FirstOrDefaultAsync(u =>
                    (u.Email != null && u.Email == request.Email) ||
                    (u.Phone != null && u.Phone == request.Phone ));

            if (existingUser == null)
                throw new InvalidOperationException("Người dùng không tồn tại");

            if(existingUser.Status != (int) UserStatus.Active)
                    throw new InvalidOperationException("Người dùng không hợp lệ");
            
            var passwordHasher = new PasswordHasher<User>();
            var result = passwordHasher.VerifyHashedPassword(
                existingUser,
                existingUser.PasswordHash,
                request.Password);
            if (result == PasswordVerificationResult.Failed)
                throw new InvalidOperationException("Mật khẩu không chính xác");

            var token = _jwtService.GenerateToken(existingUser.UserId, existingUser.FullName, existingUser.RoleType);

            return new LoginResponseDto
            {
                UserId = existingUser.UserId,
                FullName = existingUser.FullName,
                RoleType = existingUser.RoleType,
                Token = token,
            };
        }
    }

}
