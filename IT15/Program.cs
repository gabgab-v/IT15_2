using IT15.Data;
using IT15.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Resend;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// --- Determine environment ---
bool IsRunningOnRender() => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RENDER"));

// --- Database connection setup ---
string? connectionString = null;
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(databaseUrl))
{
    var databaseUri = new Uri(databaseUrl);
    var userInfo = databaseUri.UserInfo.Split(':');
    bool isInternal = !databaseUri.Host.Contains(".");

    connectionString = new NpgsqlConnectionStringBuilder
    {
        Host = databaseUri.Host,
        Port = databaseUri.Port == -1 ? 5432 : databaseUri.Port,
        Database = databaseUri.AbsolutePath.TrimStart('/'),
        Username = userInfo[0],
        Password = userInfo[1],
        SslMode = isInternal ? SslMode.Disable : SslMode.Require,
        TrustServerCertificate = true
    }.ConnectionString;

    Console.WriteLine($"Using {(isInternal ? "INTERNAL" : "EXTERNAL")} database connection: {databaseUri.Host}");
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("No database connection string found.");
    Console.WriteLine("Using connection string from appsettings");
}

// --- Add DbContext ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// --- Data Protection fix for Antiforgery ---
if (IsRunningOnRender())
{
    // Use a persistent directory on Render
    var keysDirectory = new DirectoryInfo("/opt/render/project/.keys");
    if (!keysDirectory.Exists) keysDirectory.Create();

    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(keysDirectory)
        .SetApplicationName("IT15");
    Console.WriteLine($"Data Protection keys directory: {keysDirectory.FullName}");
}
else
{
   
        builder.Services.AddDbContext<DataProtectionKeyContext>(options =>
            options.UseNpgsql(connectionString));

        builder.Services.AddDataProtection()
            .PersistKeysToDbContext<DataProtectionKeyContext>()
            .SetApplicationName("IT15");
    
}

// --- Identity ---
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// --- Other services ---
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddScoped<PayrollService>();
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddTransient<ISmsSender, SmsSender>();
builder.Services.AddHttpClient<HolidayApiService>();
builder.Services.AddHttpClient<IncomeApiService>(c => c.BaseAddress = new Uri("https://fakestoreapi.com/"));
builder.Services.AddScoped<IAuditService, AuditService>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        var path = context.Request.Path;
        context.Response.Redirect(path.StartsWithSegments("/Admin") ? "/Admin/Account/Login" :
                                  path.StartsWithSegments("/HumanResource") ? "/HumanResource/Account/Login" :
                                  path.StartsWithSegments("/Accounting") ? "/Accounting/Account/Login" :
                                  "/Identity/Account/Login");
        return Task.CompletedTask;
    };
});

builder.Services.AddControllersWithViews();

// --- Build the app ---
var app = builder.Build();

// --- Database initialization & migrations ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        if (await dbContext.Database.CanConnectAsync())
        {
            logger.LogInformation("Database connection successful!");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied.");
        }
        else
        {
            logger.LogError("Cannot connect to database!");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed");
        if (app.Environment.IsProduction() && IsRunningOnRender()) throw;
    }
}

// --- Seed data ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        await SeedData.Initialize(services, services.GetRequiredService<IConfiguration>());
        logger.LogInformation("Database seeding completed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database seeding error");
    }
}

// --- Middleware & routing ---
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
