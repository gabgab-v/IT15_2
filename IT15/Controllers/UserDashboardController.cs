using IT15.Data;
using IT15.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IT15.Controllers
{
    [Authorize] // All actions in this controller require the user to be logged in.
    public class UserDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<IdentityUser> _signInManager;

        public UserDashboardController(ApplicationDbContext context, SignInManager<IdentityUser> signInManager)
        {
            _context = context;
            _signInManager = signInManager;
        }

        // GET: /UserDashboard/Index
        public IActionResult Index() => View();

        // --- Daily Log / Attendance ---
        #region Attendance
        [HttpGet]
        public async Task<IActionResult> DailyLogs()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userLogs = await _context.DailyLogs
                                   .Where(log => log.UserId == userId)
                                   .OrderByDescending(log => log.Date)
                                   .ToListAsync();
            return View(userLogs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogAttendance()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var today = DateTime.Today;
            bool alreadyLogged = await _context.DailyLogs.AnyAsync(log => log.UserId == userId && log.Date == today);

            if (!alreadyLogged)
            {
                _context.DailyLogs.Add(new DailyLog { UserId = userId, Date = today });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("DailyLogs");
        }
        #endregion

        // --- Leave Requests ---
        #region LeaveRequests
        [HttpGet]
        public async Task<IActionResult> LeaveRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var requests = await _context.LeaveRequests
                                         .Where(r => r.RequestingEmployeeId == userId)
                                         .OrderByDescending(r => r.DateRequested)
                                         .ToListAsync();
            return View(requests);
        }

        [HttpGet]
        public IActionResult ApplyForLeave()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyForLeave(LeaveRequest leaveRequest)
        {
            // Custom validation: End date must not be before the start date.
            if (leaveRequest.EndDate < leaveRequest.StartDate)
            {
                ModelState.AddModelError(nameof(leaveRequest.EndDate), "End date cannot be before the start date.");
            }

            if (ModelState.IsValid)
            {
                leaveRequest.RequestingEmployeeId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                leaveRequest.DateRequested = DateTime.Now;
                leaveRequest.Status = LeaveRequestStatus.Pending;

                _context.Add(leaveRequest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(LeaveRequests));
            }
            // If we get here, something was invalid, so redisplay the form.
            return View(leaveRequest);
        }
        #endregion

        // --- Logout ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}


