using FixNear.DTOs.Auth;
using FixNear.Enums;
using FixNear.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Services
{
    public class AuthService : IAuthService
    {
        private readonly FixNearDbContext _context;
        public AuthService(FixNearDbContext context)
        {
            _context = context;
        }

        public async Task RegisterAsync(RegisterRequestDto request)
        {
            var existingUser = await _context.User
                .FirstOrDefaultAsync(u =>
                        (request.Email != null && u.Email == request.Email) ||
                        (request.Phone != null && u.Phone == request.Phone));

            if (existingUser != null)
                throw new InvalidOperationException("Người dùng đã tồn tại");

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender.HasValue? request.Gender.Value == Gender.Male : null,
                RoleType = (int) RoleType.Customer,
                Status = (int) UserStatus.Active
            };

            var passwordHasher = new PasswordHasher<User>();
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            
            _context.User.Add(user);       
            await _context.SaveChangesAsync();

            var userProfile = new CustomerProfile
            {
                UserId = user.UserId,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.CustomerProfile.Add(userProfile);
            await _context.SaveChangesAsync();
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

            return new LoginResponseDto
            {
                UserId = existingUser.UserId,
                FullName = existingUser.FullName,
                RoleType = existingUser.RoleType,
            };
        }
    }

}
