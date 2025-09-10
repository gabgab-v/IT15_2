using Microsoft.AspNetCore.Identity;
using System;
using System.Configuration;
using System.Threading.Tasks;

namespace IT15.Data // Make sure this namespace matches your project
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // Add "Accounting" to the list of roles to be created.
            string[] roleNames = { "Admin", "User", "HumanResource", "Accounting" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // --- The rest of your admin user creation logic remains the same ---
            var adminEmail = configuration["AppSettings:AdminUser:Email"];
            var adminPassword = configuration["AppSettings:AdminUser:Password"];

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var newAdminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var createAdminResult = await userManager.CreateAsync(newAdminUser, adminPassword);
                if (createAdminResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdminUser, "Admin");
                }
            }
        }
    }
}

