using AuthSystem.Application.Common.Exceptions;
using AuthSystem.Application.DTOs;
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

        public async Task<PagedResponseDto<UserResponseDto>> GetAllUsersAsync(
            UserFilterRequestDto request)
        {
            var query = _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .AsQueryable();

            // Search by FullName or Email
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();

                query = query.Where(u =>
                    u.FullName.Contains(search) ||
                    u.Email.Contains(search));
            }

            // Filter by Role
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var role = request.Role.Trim();

                query = query.Where(u =>
                    u.UserRoles.Any(ur => ur.Role.Name == role));
            }

            // Filter by Active/Inactive status
            if (request.IsActive.HasValue)
            {
                query = query.Where(u =>
                    u.IsActive == request.IsActive.Value);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Sorting
            if (string.Equals(
                request.SortOrder,
                "asc",
                StringComparison.OrdinalIgnoreCase))
            {
                query = request.SortBy?.ToLower() switch
                {
                    "fullname" => query.OrderBy(u => u.FullName),
                    "email" => query.OrderBy(u => u.Email),
                    "isactive" => query.OrderBy(u => u.IsActive),
                    "createdon" => query.OrderBy(u => u.CreatedOn),
                    _ => query.OrderBy(u => u.CreatedOn)
                };
            }
            else
            {
                query = request.SortBy?.ToLower() switch
                {
                    "fullname" => query.OrderByDescending(u => u.FullName),
                    "email" => query.OrderByDescending(u => u.Email),
                    "isactive" => query.OrderByDescending(u => u.IsActive),
                    "createdon" => query.OrderByDescending(u => u.CreatedOn),
                    _ => query.OrderByDescending(u => u.CreatedOn)
                };
            }

            // Pagination
            var users = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    CreatedOn = u.CreatedOn,
                    UpdatedOn = u.UpdatedOn,
                    DeletedOn = u.DeletedOn,

                    Role = u.UserRoles
                        .Select(ur => ur.Role.Name)
                        .FirstOrDefault() ?? "Employee"
                })
                .ToListAsync();

            // Return paginated response
            return new PagedResponseDto<UserResponseDto>
            {
                Items = users,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
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
            user.UpdatedOn = DateTime.UtcNow;

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
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                throw new KeyNotFoundException("User not found.");

            // Soft delete
            user.IsActive = false;
            user.DeletedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
