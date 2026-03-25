using Microsoft.AspNetCore.Identity;
using SmartEvent.Web.Models;

namespace SmartEvent.Web.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = { "Admin", "Organizer", "User" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            await CreateUser(userManager, "admin@test.com", "Admin@123", "Admin");
            await CreateUser(userManager, "organizer@test.com", "Admin@123", "Organizer");
            await CreateUser(userManager, "user@test.com", "Admin@123", "User");
        }

        private static async Task CreateUser(UserManager<ApplicationUser> userManager,
            string email, string password, string role)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email
                };

                await userManager.CreateAsync(user, password);
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}