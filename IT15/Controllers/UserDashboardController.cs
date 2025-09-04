using IT15.Data; // <-- Add this to use the DbContext
using IT15.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IT15.Controllers
{
    [Authorize]
    public class UserDashboardController : Controller
    {
        private readonly ApplicationDbContext _context; // Database context
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;


        // Inject the DbContext and UserManager
        public UserDashboardController(ApplicationDbContext context, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET: /UserDashboard/DailyLogs
        // This action now reads from the database.
        [HttpGet]
        public IActionResult DailyLogs()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Query the database for logs belonging to the current user
            var userLogs = _context.DailyLogs
                                   .Where(log => log.UserId == userId)
                                   .OrderByDescending(log => log.Date) // Show newest first
                                   .ToList();
            return View(userLogs);
        }

        // POST: /UserDashboard/LogAttendance
        // This action creates a new attendance log for the current day.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogAttendance()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var today = DateTime.Today;

            // Check if attendance has already been logged for today
            bool alreadyLogged = _context.DailyLogs.Any(log => log.UserId == userId && log.Date == today);

            if (!alreadyLogged)
            {
                var log = new DailyLog
                {
                    UserId = userId,
                    Date = today // Automatically set to the current date
                };

                _context.DailyLogs.Add(log);
                await _context.SaveChangesAsync(); // Save the new log to the database
            }

            return RedirectToAction("DailyLogs");
        }

        // DELETE THIS ACTION: Since we have no form, we don't need this anymore.
        // public IActionResult AddDailyLog() { ... }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}

