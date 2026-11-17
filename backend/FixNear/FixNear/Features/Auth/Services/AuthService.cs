using FixNear.Domain.Enums;
using FixNear.Domain.Models.Customers;
using FixNear.Domain.Models.Identity;
using FixNear.Domain.Models.Managers;
using FixNear.Domain.Models.Shops;
using FixNear.Domain.Models.Technicians;
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

        //Thông tin đăng ký
        private class RegistrationData
        {
            // Loại tài khoản
            public AccountRegistrationType RegistrationType { get; set; }

            // Thông tin User chung
            public string FullName { get; set; } = null!;
            public string Email { get; set; } = null!;
            public string? Phone { get; set; }
            public DateOnly? DateOfBirth { get; set; }
            public bool? Gender { get; set; }
            public string PasswordHash { get; set; } = null!;

            // Thông tin địa chỉ - Customer và Shop
            public int? ProvinceId { get; set; }
            public int? WardId { get; set; }
            public string? AddressLine { get; set; }
            public string? RecipientName { get; set; }
            public string? RecipientPhone { get; set; }

            // Thông tin Shop
            public string? ShopName { get; set; }
            public string? Description { get; set; }
            public string? ShopPhone { get; set; }
            public string? ShopEmail { get; set; }
            public string? TaxCode { get; set; }

            // Thông tin Technician
            public int? ExperienceYears { get; set; }
            public string? Introduction { get; set; }
        }

        //Phương thức dùng chung khi đăng ký
        private async Task RegisterAsyncCore(RegisterAccountRequestDto request, AccountRegistrationType registrationType, RegistrationData registrationData)
        {
            // Chuẩn hóa email và số điện thoại
            var email = request.Email.Trim().ToLowerInvariant();

            var phone = string.IsNullOrWhiteSpace(request.Phone)
                ? null
                : request.Phone.Trim();

            // Kiểm tra email và số điện thoại đã tồn tại
            var existingUser = await _context.User.AnyAsync(u =>
                (u.Email != null && u.Email.ToLower() == email) ||
                (phone != null && u.Phone == phone));

            if (existingUser)
                throw new InvalidOperationException("Email hoặc số điện thoại đã được sử dụng");

            // Kiểm tra địa chỉ đối với Customer và Shop
            if (registrationType == AccountRegistrationType.Customer || registrationType == AccountRegistrationType.Shop)
            {
                if (!registrationData.ProvinceId.HasValue || !registrationData.WardId.HasValue ||
                    string.IsNullOrWhiteSpace(registrationData.AddressLine) ||
                    string.IsNullOrWhiteSpace(registrationData.RecipientName) ||
                    string.IsNullOrWhiteSpace(registrationData.RecipientPhone))
                        throw new InvalidOperationException("Thông tin địa chỉ không hợp lệ");

                var validWard = await _context.Ward.AnyAsync(w =>
                    w.WardId == registrationData.WardId.Value &&
                    w.ProvinceId == registrationData.ProvinceId.Value);

                if (!validWard)
                    throw new InvalidOperationException("Phường/Xã không thuộc Tỉnh/Thành phố đã chọn");
            }

            var now = DateTime.UtcNow;

            // Tìm những yêu cầu đăng ký OTP chưa sử dụng
            var oldVerifications = await _context.EmailVerifications
                .Where(e =>
                    e.Email == email &&
                    e.Purpose == EmailVerificationPurpose.Register &&
                    !e.IsUsed)
                .ToListAsync();

            // Kiểm tra thời gian chờ 60 giây
            var latestVerification = oldVerifications
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefault();

            if (latestVerification != null &&
                latestVerification.CreatedAt.AddSeconds(60) > now)
            {
                throw new InvalidOperationException(
                    "Vui lòng chờ 60 giây trước khi yêu cầu gửi lại OTP");
            }

            //Hash mật khẩu
            var passwordHasher = new PasswordHasher<User>();
            var temporaryUser = new User();

            var passwordHash = passwordHasher.HashPassword( temporaryUser, request.Password);

            //Bổ sung thông tin chung vào RegistrationData
            registrationData.RegistrationType = registrationType;
            registrationData.FullName = request.FullName;
            registrationData.Email = email;
            registrationData.Phone = phone;
            registrationData.DateOfBirth = request.DateOfBirth;

            registrationData.Gender = request.Gender.HasValue
                ? request.Gender.Value == Gender.Male
                : null;

            registrationData.PasswordHash = passwordHash;

            //Vô hiệu hóa OTP cũ
            foreach (var verification in oldVerifications)
                verification.IsUsed = true;

            //Tạo OTP mới
            var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

            // Tạo bản ghi EmailVerification
            var verificationRequest = new EmailVerification
            {
                Email = email,
                Purpose = EmailVerificationPurpose.Register,
                RegistrationType = registrationType,
                RegistrationData = JsonSerializer.Serialize(registrationData),
                ExpiresAt = now.AddMinutes(5),
                CreatedAt = now,
                FailedAttempts = 0,
                IsUsed = false
            };

            //Hash OTP
            var otpHasher = new PasswordHasher<EmailVerification>();

            verificationRequest.OtpHash = otpHasher.HashPassword( verificationRequest, otp);

            //Lưu yêu cầu xác thực
            _context.EmailVerifications.Add(verificationRequest);

            await _context.SaveChangesAsync();

            // Gửi OTP qua email
            await _emailService.SendVerificationCodeAsync(email, otp);
        }

        //Đăng ký Customer
        public Task RegisterAsync(RegisterCustomerRequestDto request)
        {
            var registrationData = new RegistrationData
            {
                ProvinceId = request.ProvinceId,
                WardId = request.WardId,
                AddressLine = request.AddressLine,
                RecipientName = request.RecipientName,
                RecipientPhone = request.RecipientPhone
            };

            return RegisterAsyncCore(request, AccountRegistrationType.Customer, registrationData);
        }

        //Đăng ký Shop
        public Task RegisterAsync(RegisterShopRequestDto request)
        {
            var registrationData = new RegistrationData
            {
                ProvinceId = request.ProvinceId,
                WardId = request.WardId,
                AddressLine = request.AddressLine,
                RecipientName = request.RecipientName ?? request.FullName,
                RecipientPhone = request.RecipientPhone
                    ?? request.ShopPhone
                    ?? request.Phone,
                ShopName = request.ShopName,
                Description = request.Description,
                ShopPhone = request.ShopPhone,
                ShopEmail = request.ShopEmail,
                TaxCode = request.TaxCode
            };

            return RegisterAsyncCore( request, AccountRegistrationType.Shop, registrationData);
        }

        //Đăng ký Technician
        public Task RegisterAsync(RegisterTechnicianRequestDto request)
        {
            var registrationData = new RegistrationData
            {
                ExperienceYears = request.ExperienceYears,
                Introduction = request.Introduction
            };

            return RegisterAsyncCore(request, AccountRegistrationType.Technician, registrationData);
        }

        //Xác thực email
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

                if (verification.FailedAttempts >= 5)
                    verification.IsUsed = true;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                throw new InvalidOperationException( verification.FailedAttempts >= 5 ? "Bạn đã nhập sai OTP quá số lần cho phép" : "Mã OTP không chính xác");
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

            //Kiểm tra loại tài khoản
            if(registrationData.RegistrationType != verification.RegistrationType)
            {
                verification.IsUsed = true;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                throw new InvalidOperationException("Loại tài khoản đăng ký không hợp lệ");
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

            //Xác định RoleType
            var roleType = registrationData.RegistrationType switch
            {
                AccountRegistrationType.Customer => RoleType.Customer,
                AccountRegistrationType.Shop => RoleType.Manager,
                AccountRegistrationType.Technician => RoleType.Technician,
                _ => throw new InvalidOperationException("Loại tài khoản không được hỗ trợ")
            };

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

            // Tạo hồ sơ và dữ liệu theo loại tài khoản
            switch (registrationData.RegistrationType)
            {
                case AccountRegistrationType.Customer:
                    {
                        // Tạo CustomerProfile
                        var customerProfile = new CustomerProfile
                        {
                            UserId = user.UserId,
                            CreatedAt = now
                        };

                        _context.CustomerProfile.Add(customerProfile);
                        await _context.SaveChangesAsync();

                        // Tạo địa chỉ Customer
                        var address = new Address
                        {
                            OwnerType = 1,
                            CustomerId = customerProfile.CustomerId,
                            ProvinceId = registrationData.ProvinceId!.Value,
                            WardId = registrationData.WardId!.Value,
                            AddressLine = registrationData.AddressLine!,
                            RecipientName = registrationData.RecipientName,
                            RecipientPhone = registrationData.RecipientPhone,
                            IsLocked = false,
                            CreatedAt = now
                        };

                        _context.Address.Add(address);
                        await _context.SaveChangesAsync();

                        // Đặt địa chỉ mặc định
                        customerProfile.DefaultAddressId = address.AddressId;

                        await _context.SaveChangesAsync();
                        break;
                    }

                case AccountRegistrationType.Shop:
                    {
                        // Tạo ManagerProfile
                        var managerProfile = new ManagerProfile
                        {
                            UserId = user.UserId,
                            CreatedAt = now
                        };

                        _context.ManagerProfile.Add(managerProfile);
                        await _context.SaveChangesAsync();

                        // Tạo địa chỉ Shop
                        var address = new Address
                        {
                            OwnerType = 2,
                            CustomerId = null,
                            ProvinceId = registrationData.ProvinceId!.Value,
                            WardId = registrationData.WardId!.Value,
                            AddressLine = registrationData.AddressLine!,
                            RecipientName = registrationData.RecipientName,
                            RecipientPhone = registrationData.RecipientPhone,
                            IsLocked = false,
                            CreatedAt = now
                        };

                        _context.Address.Add(address);
                        await _context.SaveChangesAsync();

                        // Tạo Shop
                        var shop = new Shop
                        {
                            ManagerId = managerProfile.ManagerId,
                            AddressId = address.AddressId,
                            ShopName = registrationData.ShopName!,
                            Description = registrationData.Description,
                            Phone = registrationData.ShopPhone,
                            Email = registrationData.ShopEmail,
                            TaxCode = registrationData.TaxCode,
                            RatingAverage = 0,
                            Status = 1,
                            IsVerified = false,
                            CreatedAt = now
                        };

                        _context.Shop.Add(shop);
                        await _context.SaveChangesAsync();

                        // Tạo thành viên Owner cho Shop
                        var shopMember = new ShopMember
                        {
                            ShopId = shop.ShopId,
                            ManagerId = managerProfile.ManagerId,
                            ShopRole = Enums.ShopRole.Owner,
                            JoinedAt = now,
                            LeftAt = null,
                            Status = Enums.ShopMemberStatus.Active,
                            CreatedAt = now
                        };

                        _context.ShopMember.Add(shopMember);
                        await _context.SaveChangesAsync();

                        break;
                    }

                case AccountRegistrationType.Technician:
                    {
                        // Tạo TechnicianProfile
                        var technicianProfile = new TechnicianProfile
                        {
                            UserId = user.UserId,
                            ExperienceYears = registrationData.ExperienceYears,
                            Introduction = registrationData.Introduction,
                            TechnicianStatus = 1,
                            RatingAverage = 0,
                            CreatedAt = now
                        };

                        _context.TechnicianProfile.Add(technicianProfile);
                        await _context.SaveChangesAsync();

                        break;
                    }

                default:
                    throw new InvalidOperationException("Loại tài khoản không được hỗ trợ");
            }

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
