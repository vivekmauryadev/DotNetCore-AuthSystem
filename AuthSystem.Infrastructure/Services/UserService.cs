using AuthSystem.Application.Common.Exceptions;
using AuthSystem.Application.DTOs.Users;
using AuthSystem.Application.Interfaces;
using AuthSystem.Domain.Entities;
using AuthSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthSystem.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
        {
            return await _context.Users
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .Select(user => new UserResponseDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    IsActive = user.IsActive,
                    CreatedOn = user.CreatedOn,
                    Role = user.UserRoles
                        .Select(x => x.Role.Name)
                        .FirstOrDefault() ?? "Employee"
                })
                .ToListAsync();
        }

        public async Task<UserResponseDto?> GetUserByIdAsync(int id)
        {
            return await _context.Users
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .Where(x => x.Id == id)
                .Select(user => new UserResponseDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    IsActive = user.IsActive,
                    CreatedOn = user.CreatedOn,
                    Role = user.UserRoles
                        .Select(x => x.Role.Name)
                        .FirstOrDefault() ?? "Employee"
                })
                .FirstOrDefaultAsync();
        }

        public async Task<UserResponseDto?> UpdateUserAsync(
            int id,
            UpdateUserRequestDto request)
        {
            var user = await _context.Users
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (user == null)
                return null;

            var emailExists = await _context.Users
                .AnyAsync(x => x.Email == request.Email && x.Id != id);

            if (emailExists)
            {
                throw new ConflictException(
                    "Email address is already registered with another user.");
            }

            user.FullName = request.FullName;
            user.Email = request.Email;
            user.IsActive = request.IsActive;

            var role = await _context.Roles
                .FirstOrDefaultAsync(x => x.Name == request.Role);

            if (role == null)
            {
                throw new InvalidOperationException(
                    $"Role '{request.Role}' does not exist.");
            }

            var existingUserRole = user.UserRoles.FirstOrDefault();

            if (existingUserRole != null)
            {
                existingUserRole.RoleId = role.Id;
            }
            else
            {
                user.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id
                });
            }

            await _context.SaveChangesAsync();

            return await GetUserByIdAsync(id);
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == id);

            if (user == null)
                return false;

            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
