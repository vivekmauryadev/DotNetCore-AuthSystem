using AuthSystem.Application.DTOs;
using AuthSystem.Application.DTOs.Users;
using AuthSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] UserFilterRequestDto request)
        {
            var result = await _userService.GetAllUsersAsync(request);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);

            if (user == null)
                return NotFound(new
                {
                    Message = "User not found."
                });

            return Ok(user);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateUser(
            int id,
            [FromBody] UpdateUserRequestDto request)
        {
            var user = await _userService.UpdateUserAsync(id, request);

            if (user == null)
                return NotFound(new
                {
                    Message = "User not found."
                });

            return Ok(user);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result = await _userService.DeleteUserAsync(id);

            return Ok(new
            {
                message = "User deleted successfully.",
                success = result
            });
        }
    }
}
