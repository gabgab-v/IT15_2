using IT15.Models; // <-- Add this to use your new model
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims; // <-- Add this to get the user's ID
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;


namespace IT15.Controllers // <-- Make sure this namespace matches your project
{
    [Authorize] // Ensures only logged-in users can access this
    public class UserDashboardController : Controller
    {
        // Temporary in-memory database. A real database would be used here.
        private static List<DailyLog> _dailyLogs = new List<DailyLog>();
        private readonly SignInManager<IdentityUser> _signInManager;

        public UserDashboardController(SignInManager<IdentityUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        // *** NEW ACTION METHODS FOR DAILY LOGS ***

        // GET: /UserDashboard/DailyLogs
        // This action shows the list of all logs for the current user.
        [HttpGet]
        public IActionResult DailyLogs()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userLogs = _dailyLogs.Where(log => log.UserId == userId).ToList();
            return View(userLogs);
        }

        // GET: /UserDashboard/AddDailyLog
        // This action shows the form to add a new log.
        [HttpGet]
        public IActionResult AddDailyLog()
        {
            return View();
        }

        // POST: /UserDashboard/AddDailyLog
        // This action handles the form submission.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddDailyLog(DailyLog dailyLog)
        {
            if (ModelState.IsValid)
            {
                dailyLog.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                dailyLog.Id = _dailyLogs.Count + 1; // Simple ID generation
                _dailyLogs.Add(dailyLog);

                return RedirectToAction("DailyLogs"); // Redirect back to the list
            }
            return View(dailyLog); // If form is invalid, show it again with errors
        }

        // Action for handling logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home"); // Redirect to home page after logout
        }
    }
}

