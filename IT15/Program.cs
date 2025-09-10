using IT15.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddScoped<IT15.Services.PayrollService>();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/Admin"))
        {
            context.Response.Redirect("/Admin/Account/Login");
        }
        else if (context.Request.Path.StartsWithSegments("/HumanResource"))
        {
            context.Response.Redirect("/HumanResource/Account/Login");
        }
        else if (context.Request.Path.StartsWithSegments("/Accounting"))
        {
            context.Response.Redirect("/Accounting/Account/Login");
        }
        else
        {
            context.Response.Redirect("/Identity/Account/Login");
        }
        return Task.CompletedTask;
    };
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        // THE FIX: This line is now active and will call your seeder.
        await SeedData.Initialize(services, configuration);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
// THIS IS THE MOST IMPORTANT SECTION FOR DEBUGGING
if (app.Environment.IsDevelopment())
{
    // This will replace the "page not exist" error with a detailed error report.
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

// The order of these three lines is CRITICAL for routing and security to work.
app.UseRouting();
app.UseAuthentication(); // <-- This was likely missing and is required.
app.UseAuthorization();

app.MapControllerRoute(
    name: "AccountingArea",
    pattern: "{area:exists}/{controller=Accounting}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "HumanResource",
    pattern: "{area:exists}/{controller=LeaveRequest}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "AdminArea", // Renamed for clarity
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

