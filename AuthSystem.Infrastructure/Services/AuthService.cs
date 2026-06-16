using AuthSystem.Application.DTOs.Auth;
using AuthSystem.Application.Interfaces;
using AuthSystem.Domain.Entities;
using AuthSystem.Infrastructure.Authentication;
using Microsoft.Extensions.Configuration;
using AuthSystem.Application.DTOs.Auth;
using AuthSystem.Application.Interfaces;
using AuthSystem.Domain.Entities;
using AuthSystem.Infrastructure.Authentication;
using AuthSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthSystem.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto> RegisterAsync(
            RegisterRequestDto request)
        {
            var existingUser =
                await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.Email == request.Email);

            if (existingUser != null)
                throw new Exception("User already exists.");

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        request.Password),
                CreatedOn = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var role = await _context.Roles
                .FirstOrDefaultAsync(x =>
                    x.Name == request.RoleName);

            if (role != null)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id
                });

                await _context.SaveChangesAsync();
            }

            var token = await GenerateJwtToken(user);

            return token;
        }

        public async Task<AuthResponseDto> LoginAsync(
            LoginRequestDto request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.Email == request.Email);

            if (user == null)
                throw new Exception("Invalid credentials.");

            var isPasswordValid =
                BCrypt.Net.BCrypt.Verify(
                    request.Password,
                    user.PasswordHash);

            if (!isPasswordValid)
                throw new Exception("Invalid credentials.");

            return await GenerateJwtToken(user);
        }

        private async Task<AuthResponseDto>
            GenerateJwtToken(User user)
        {
            var jwtSettings =
                _configuration
                    .GetSection("Jwt")
                    .Get<JwtSettings>();

            var userRole = await _context.UserRoles
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x =>
                    x.UserId == user.Id);

            var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),

            new(JwtRegisteredClaimNames.Email,
                user.Email),

            new(ClaimTypes.Name,
                user.FullName),

            new(ClaimTypes.Role,
                userRole?.Role?.Name ?? "Employee")
        };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings!.Key));

            var credentials =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256);

            var expiration =
                DateTime.UtcNow.AddMinutes(
                    jwtSettings.DurationInMinutes);

            var token = new JwtSecurityToken(
                issuer: jwtSettings.Issuer,
                audience: jwtSettings.Audience,
                claims: claims,
                expires: expiration,
                signingCredentials: credentials);

            return new AuthResponseDto
            {
                Token =
                    new JwtSecurityTokenHandler()
                        .WriteToken(token),

                Expiration = expiration
            };
        }
    }
}
