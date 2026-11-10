using FixNear.DTOs.Auth;
using FixNear.Enums;
using FixNear.Models;
using FixNear.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Services
{
    public class AuthService : IAuthService
    {
        private readonly FixNearDbContext _context;
        private readonly IJwtService _jwtService;
        public AuthService(FixNearDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        public async Task RegisterAsync(RegisterRequestDto request)
        {
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

            //Transaction để đảm bảo tính toàn vẹn dữ liệu
            await using var transaction = await _context.Database.BeginTransactionAsync();

            //Tạo user
            try { 
                var user = new User
                {
                    FullName = request.FullName,
                    Email = request.Email,
                    Phone = request.Phone,
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender.HasValue? request.Gender.Value == Gender.Male : null,
                    RoleType = (int) RoleType.Customer,
                    Status = (int)UserStatus.Active
                };

                var passwordHasher = new PasswordHasher<User>();
                user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            
                _context.User.Add(user);       
                await _context.SaveChangesAsync();

                //Tạo profile cho user
                var userProfile = new CustomerProfile
                {
                    UserId = user.UserId,
                    CreatedAt = DateTime.UtcNow
                };
            
                _context.CustomerProfile.Add(userProfile);
                await _context.SaveChangesAsync();

                //Tạo địa chỉ cho user
                var address = new Address
                {
                    OwnerType = (int)RoleType.Customer,
                    CustomerId = userProfile.CustomerId,
                    ProvinceId = request.ProvinceId,
                    WardId = request.WardId,
                    AddressLine = request.AddressLine,
                    RecipientName = request.FullName,
                    IsLocked = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Address.Add(address);
                await _context.SaveChangesAsync();

                // Đặt địa chỉ vừa tạo làm địa chỉ mặc định
                userProfile.DefaultAddressId = address.AddressId;

                await _context.SaveChangesAsync();

                // 8. Hoàn tất transaction
                await transaction.CommitAsync();
            }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
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

        public async Task<CurrentUserResponseDto> GetCurrentUserAsync(int userId)
        {
            var user = await _context.User.FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                throw new InvalidOperationException("Người dùng không tồn tại");

            return new CurrentUserResponseDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                RoleType = user.RoleType
            };
        }
    }

}
