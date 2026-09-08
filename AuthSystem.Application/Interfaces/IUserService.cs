using AuthSystem.Application.DTOs.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthSystem.Application.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();

        Task<UserResponseDto?> GetUserByIdAsync(int id);

        Task<UserResponseDto?> UpdateUserAsync(
            int id,
            UpdateUserRequestDto request);

        Task<bool> DeleteUserAsync(int id);
    }
}
