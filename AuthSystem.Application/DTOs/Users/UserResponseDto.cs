using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthSystem.Application.DTOs.Users
{
    public class UserResponseDto
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public DateTime? DeletedOn { get; set; }

        public string Role { get; set; } = string.Empty;
    }
}
