using EcommerceApi.Dal;
using EcommerceApi.Dal.Entities;
using EcommerceApi.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly EcomDbContext _context;
        public AuthController(EcomDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            if (request.Email == null || request.Password == null)
            {
                return BadRequest("Email and password are required.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return Unauthorized("Invalid email or password.");
            }

            // Here you would typically verify the password
            // For example: if (!VerifyPassword(request.Password, user.PasswordHash)) { return Unauthorized("Invalid email or password."); }

            return Ok(new { Message = "Login successful", UserId = user.Id });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            if (request.Email == null || request.Password == null || request.ConfirmPassword == null)
            {
                return BadRequest("Email, password and confirm password are required.");
            }

            if (request.Password != request.ConfirmPassword)
            {
                return BadRequest("Passwords do not match.");
            }

            var existingUser = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (existingUser)
            {
                return BadRequest("Email is already in use.");
            }

            var user = new User
            {
                FirstName = request.firstName,
                LastName = request.lastName,
                Email = request.Email,
                Username = request.Username,
                PasswordHash = HashPassword(request.Password) // You would hash the password here
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Registration successful", UserId = user.Id });
        }

        private string HashPassword(string password)
        {
            // Implement your password hashing logic here
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}
