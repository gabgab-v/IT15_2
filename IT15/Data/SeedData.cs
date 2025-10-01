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
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                var createAdminResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (createAdminResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
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

            if (!await context.CompanyLedger.AnyAsync(c => c.Description == "Initial Capital"))
            {
                context.CompanyLedger.Add(new CompanyLedger
                {
                    UserId = adminUser.Id, // Attribute initial funds to the Admin user
                    TransactionDate = DateTime.UtcNow,
                    Description = "Initial Capital",
                    Amount = 500000.00m
                });
                await context.SaveChangesAsync();
            }

            // --- Seed Suppliers and Supplies ---
            if (!await context.Supplies.AnyAsync())
            {
                // Pick an existing supplier (or create one if none exists)
                var supplier = await context.Supplier.FirstOrDefaultAsync();
                if (supplier == null)
                {
                    supplier = new Supplier { Name = "API General Supplier" };
                    context.Supplier.Add(supplier);
                    await context.SaveChangesAsync();
                }

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                    try
                    {
                        var productsFromApi = await httpClient.GetFromJsonAsync<List<StoreProduct>>("https://fakestoreapi.com/products");
                        if (productsFromApi != null)
                        {
                            foreach (var product in productsFromApi)
                            {
                                context.Supplies.Add(new Supply { Name = product.Title, SupplierId = supplier.Id, StockLevel = 0, Cost = product.Price });
                            }
                            await context.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Could not seed supplies from API: {ex.Message}");
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

