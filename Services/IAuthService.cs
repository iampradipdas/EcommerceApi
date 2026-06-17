using EcommerceApi.Dal;
using EcommerceApi.Dal.Entities;
using EcommerceApi.DTOs;
using Microsoft.EntityFrameworkCore;
using System;

namespace EcommerceApi.Services
{
    // Services/IAuthService.cs
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
    }

    // Services/AuthService.cs
    public class AuthService : IAuthService
    {
        private readonly EcomDbContext _db;
        private readonly IJwtService _jwt;
        private readonly IConfiguration _config;

        public AuthService(EcomDbContext db, IJwtService jwt, IConfiguration config)
        {
            _db = db;
            _jwt = jwt;
            _config = config;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            // 1. Check if email already exists
            var exists = await _db.Users.AnyAsync(u => u.Email == dto.Email);
            if (exists)
                throw new InvalidOperationException("Email already registered.");

            // 2. Hash the password (never store plain text)
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // 3. Create user entity
            var user = new User
            {
                Email = dto.Email,
                PasswordHash = hashedPassword,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PhoneNumber = dto.PhoneNumber,
                Role = "Customer",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // 4. Generate JWT and return
            return BuildResponse(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            // 1. Find user by email
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                throw new UnauthorizedAccessException("Invalid email or password.");

            // 2. Check if account is active
            if ((bool)!user.IsActive)
                throw new UnauthorizedAccessException("Account is disabled.");

            // 3. Verify password against hash
            bool isValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!isValid)
                throw new UnauthorizedAccessException("Invalid email or password.");

            // 4. Generate JWT and return
            return BuildResponse(user);
        }

        private AuthResponseDto BuildResponse(User user)
        {
            var expiryMins = int.Parse(_config["JwtSettings:ExpiryMinutes"]!);
            var token = _jwt.GenerateToken(user);

            return new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                FullName = $"{user.FirstName} {user.LastName}",
                Role = user.Role,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMins)
            };
        }
    }
}
