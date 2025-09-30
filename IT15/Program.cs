using IT15.Data;
using IT15.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Resend;
using Npgsql;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Function to determine if we're running on Render
bool IsRunningOnRender() => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RENDER"));

// Get connection string - prioritize environment variables over appsettings
string? connectionString = null;

// Check for DATABASE_URL (Render's PostgreSQL URL format)
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var databaseUrlInternal = Environment.GetEnvironmentVariable("DATABASE_URL_INTERNAL");

// Prefer internal URL when available (better performance on Render)
var urlToUse = !string.IsNullOrEmpty(databaseUrlInternal) ? databaseUrlInternal : databaseUrl;

if (!string.IsNullOrEmpty(urlToUse))
{
    try
    {
        var databaseUri = new Uri(urlToUse);
        var userInfo = databaseUri.UserInfo.Split(':');

        // Check if it's internal connection (no dots in hostname)
        bool isInternal = !databaseUri.Host.Contains(".");

        var npgsqlConnStr = new NpgsqlConnectionStringBuilder
        {
            Host = databaseUri.Host,
            Port = databaseUri.Port == -1 ? 5432 : databaseUri.Port,
            Database = databaseUri.AbsolutePath.TrimStart('/'),
            Username = userInfo[0],
            Password = userInfo[1],
            // Internal connections don't need SSL
            SslMode = isInternal ? SslMode.Disable : SslMode.Require,
            TrustServerCertificate = true
        };

        connectionString = npgsqlConnStr.ConnectionString;

        // Log which connection type we're using
        var logger = builder.Services.BuildServiceProvider().GetService<ILogger<Program>>();
        Console.WriteLine($"Using {(isInternal ? "INTERNAL" : "EXTERNAL")} database connection");
        Console.WriteLine($"Host: {databaseUri.Host}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error parsing DATABASE_URL: {ex.Message}");
    }
}

// Fall back to appsettings if no environment variable is set
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    Console.WriteLine("Using connection string from appsettings");
}

// Validate we have a connection string
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException(
        "No database connection string found. " +
        "Please set DATABASE_URL, DATABASE_URL_INTERNAL, or configure DefaultConnection in appsettings.");
}

// Add DbContext - ONLY ONCE
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddScoped<IT15.Services.PayrollService>();

// Configure Data Protection
if (IsRunningOnRender())
{
    // On Render, use a persistent directory
    var keysDirectory = new DirectoryInfo("/opt/render/project/.keys");
    if (!keysDirectory.Exists)
    {
        keysDirectory.Create();
    }

    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(keysDirectory)
        .SetApplicationName("IT15");

    Console.WriteLine($"Data Protection keys directory: {keysDirectory.FullName}");
}
else
{
    // Local development - use default
    builder.Services.AddDataProtection()
        .SetApplicationName("IT15");
}

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Configuration
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();

builder.Logging.AddConsole();

// Register services
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddTransient<ISmsSender, SmsSender>();
builder.Services.AddHttpClient<HolidayApiService>();
builder.Services.AddHttpClient<IncomeApiService>(client =>
{
    client.BaseAddress = new Uri("https://fakestoreapi.com/");
});
builder.Services.AddScoped<IAuditService, AuditService>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/Admin"))
            context.Response.Redirect("/Admin/Account/Login");
        else if (context.Request.Path.StartsWithSegments("/HumanResource"))
            context.Response.Redirect("/HumanResource/Account/Login");
        else if (context.Request.Path.StartsWithSegments("/Accounting"))
            context.Response.Redirect("/Accounting/Account/Login");
        else
            context.Response.Redirect("/Identity/Account/Login");
        return Task.CompletedTask;
    };
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Test database connection and apply migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();

        // Test connection
        var canConnect = await dbContext.Database.CanConnectAsync();
        if (canConnect)
        {
            logger.LogInformation("Successfully connected to database");
            Console.WriteLine(" Database connection successful!");

            // Apply migrations
            logger.LogInformation("Applying database migrations...");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations completed");
            Console.WriteLine(" Migrations applied successfully!");
        }
        else
        {
            logger.LogError("Cannot connect to database");
            Console.WriteLine(" Failed to connect to database!");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed");
        Console.WriteLine($" Database error: {ex.Message}");

        // In production, we might want to fail fast
        if (app.Environment.IsProduction() && IsRunningOnRender())
        {
            throw; // This will cause the deployment to fail, which is what we want
        }
    }
}

// Seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        await SeedData.Initialize(services, configuration);
        logger.LogInformation("Database seeding completed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database");
        // Don't fail the app for seeding errors
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

Console.WriteLine($"Application starting in {app.Environment.EnvironmentName} mode");

app.Run();