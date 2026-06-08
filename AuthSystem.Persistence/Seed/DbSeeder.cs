using AuthSystem.Domain.Entities;
using AuthSystem.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthSystem.Persistence.Seed
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(
            ApplicationDbContext context)
        {
            if (!context.Roles.Any())
            {
                var roles = new List<Role>
            {
                new() { Name = "Admin" },
                new() { Name = "Manager" },
                new() { Name = "Employee" }
            };

                await context.Roles.AddRangeAsync(roles);
                await context.SaveChangesAsync();
            }
        }
    }
}
