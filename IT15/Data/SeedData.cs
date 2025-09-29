using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore; // THE FIX: This using statement is required for .AnyAsync()
using Microsoft.Extensions.Configuration;
using IT15.Models;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http; // Required for HttpClient
using System.Net.Http.Json;

namespace IT15.Data // Make sure this namespace matches your project
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

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

            if (!await context.DeliveryServices.AnyAsync())
            {
                context.DeliveryServices.AddRange(
                    new DeliveryService { Name = "LBC Express", Fee = 150.00m },
                    new DeliveryService { Name = "J&T Express", Fee = 120.50m },
                    new DeliveryService { Name = "GrabExpress", Fee = 250.00m }
                );
                await context.SaveChangesAsync();
            }

            if (!await context.Supplier.AnyAsync())
            {
                var supplier = new Supplier { Name = "API General Supplier" };
                context.Supplier.Add(supplier);
                await context.SaveChangesAsync(); // Save to get the supplier ID

                // THE CHANGE: Fetch product data directly from the API
                using (var httpClient = new HttpClient())
                {
                    try
                    {
                        var productsFromApi = await httpClient.GetFromJsonAsync<List<StoreProduct>>("https://fakestoreapi.com/products");

                        if (productsFromApi != null)
                        {
                            foreach (var product in productsFromApi)
                            {
                                // Use the API data to create the local supply record
                                context.Supplies.Add(new Supply
                                {
                                    Name = product.Title,
                                    SupplierId = supplier.Id,
                                    StockLevel = 0,
                                    Cost = product.Price // Use the dynamic price from the API
                                });
                            }
                            await context.SaveChangesAsync();
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        // In a real app, you would log this error.
                        // This demonstrates the reliability issue with calling APIs at startup.
                        Console.WriteLine($"Could not fetch products from API during seeding: {ex.Message}");
                    }
                }
            }

            var testUser = await userManager.FindByEmailAsync("gabs3@gmail.com");

            if (testUser != null)
            {
                // --- MODIFIABLE PARAMETERS FOR TESTING ---
                // Change these values to populate data for a different month or year.
                int yearToSeed = 2025;
                int monthToSeed = 8; // 8 = August
                // -----------------------------------------

                var random = new Random();
                int daysInMonth = DateTime.DaysInMonth(yearToSeed, monthToSeed);

                for (int i = 1; i <= daysInMonth; i++)
                {
                    var date = new DateTime(yearToSeed, monthToSeed, i);

                    if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                    {
                        continue;
                    }

                    // THE FIX: Check if a log for this specific day already exists for the user.
                    bool logExists = await context.DailyLogs
                        .AnyAsync(d => d.UserId == testUser.Id && d.CheckInTime.Date == date.Date);

                    // Only add a new log if one doesn't already exist for this day.
                    if (!logExists)
                    {
                        var checkInTime = date.AddHours(8).AddMinutes(random.Next(0, 59));
                        var checkOutTime = date.AddHours(17).AddMinutes(random.Next(0, 30));

                        context.DailyLogs.Add(new DailyLog
                        {
                            UserId = testUser.Id,
                            CheckInTime = checkInTime,
                            CheckOutTime = checkOutTime,
                            Status = AttendanceStatus.Present,
                            OvertimeStatus = OvertimeStatus.NotApplicable
                        });
                    }
                }
                await context.SaveChangesAsync();
            }
        }
    }
}

