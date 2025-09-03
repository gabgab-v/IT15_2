using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

// IMPORTANT: Update this namespace to match your project's name
namespace YourProjectName.Data
{
    public static class SeedData
    {
        // This method seeds the database with initial roles and a default admin user.
        public static async Task Initialize(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // --- Create Roles ---
            string[] roleNames = { "Admin", "User" };
            foreach (var roleName in roleNames)
            {
                // Check if the role already exists
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    // Create the new role
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // --- Create a Default Admin User ---
            var adminEmail = configuration["AppSettings:AdminUser:Email"];
            var adminPassword = configuration["AppSettings:AdminUser:Password"];

            if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
            {
                // In a real app, you might throw an exception if the config is missing.
                Console.WriteLine("Admin user credentials not in appsettings.json. Skipping admin creation.");
                return;
            }

            // Check if the admin user already exists
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                var newAdminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true // Bypass email confirmation for the seed user
                };

                // Create the user with the specified password
                var createAdminResult = await userManager.CreateAsync(newAdminUser, adminPassword);
                if (createAdminResult.Succeeded)
                {
                    // Assign the "Admin" role to the new user
                    // This is the crucial step that links the user to the role.
                    await userManager.AddToRoleAsync(newAdminUser, "Admin");
                }
            }
        }
    }
}
