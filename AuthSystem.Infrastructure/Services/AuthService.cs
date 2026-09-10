using AuthSystem.Application.Common.Exceptions;
using AuthSystem.Application.DTOs.Auth;
using AuthSystem.Application.DTOs.Auth;
using AuthSystem.Application.Interfaces;
using AuthSystem.Application.Interfaces;
using AuthSystem.Domain.Entities;
using AuthSystem.Domain.Entities;
using AuthSystem.Infrastructure.Authentication;
using AuthSystem.Infrastructure.Authentication;
using AuthSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.Email == request.Email);

            if (existingUser != null)
            {
                throw new ConflictException(
                    "User with this email already exists.");
            }

            var role = await _context.Roles
                .FirstOrDefaultAsync(x =>
                    x.Name == request.RoleName);

            if (role == null)
            {
                throw new BusinessException(
                    $"Role '{request.RoleName}' does not exist.");
            }

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        request.Password),
                CreatedOn = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            _context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });

            await _context.SaveChangesAsync();

            return await GenerateJwtToken(user);
        }

        public async Task<AuthResponseDto> LoginAsync(
            LoginRequestDto request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.Email == request.Email &&
                    x.IsActive);

            if (user == null)
                throw new BusinessException(
                    "Invalid credentials.");

            var isPasswordValid =
                BCrypt.Net.BCrypt.Verify(
                    request.Password,
                    user.PasswordHash);

            if (!isPasswordValid)
                throw new BusinessException(
                    "Invalid credentials.");

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

                new(ClaimTypes.NameIdentifier,
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

            var refreshToken = GenerateRefreshToken();

            var refreshTokenExpiration =
                DateTime.UtcNow.AddDays(7);

            _context.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiryDate = refreshTokenExpiration,
                IsRevoked = false
            });

            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Token =
                    new JwtSecurityTokenHandler()
                        .WriteToken(token),

                Expiration = expiration,

                RefreshToken = refreshToken,

                RefreshTokenExpiration = refreshTokenExpiration
            };
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];

            using var randomNumberGenerator =
                RandomNumberGenerator.Create();

            randomNumberGenerator.GetBytes(randomBytes);

            return Convert.ToBase64String(randomBytes);
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(
            RefreshTokenRequestDto request)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(x =>
                    x.Token == request.RefreshToken);

            if (refreshToken == null)
                throw new BusinessException(
                    "Invalid refresh token.");

            if (refreshToken.IsRevoked)
                throw new BusinessException(
                    "Refresh token has already been revoked.");

            if (refreshToken.ExpiryDate <= DateTime.UtcNow)
                throw new BusinessException(
                    "Refresh token has expired.");

            var user = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.Id == refreshToken.UserId);

            if (user == null)
                throw new BusinessException(
                    "User associated with refresh token was not found.");

            // Revoke old refresh token
            refreshToken.IsRevoked = true;

            await _context.SaveChangesAsync();

            // Generate new access token + refresh token
            return await GenerateJwtToken(user);
        }

        public async Task RevokeRefreshTokenAsync(
            string token)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(x =>
                    x.Token == token);

            if (refreshToken == null)
                throw new InvalidOperationException(
                    "Invalid refresh token.");

            if (refreshToken.IsRevoked)
                return;

            refreshToken.IsRevoked = true;

            await _context.SaveChangesAsync();
        }
    }
}
