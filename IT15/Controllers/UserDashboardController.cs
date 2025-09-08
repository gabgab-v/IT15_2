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
    [Authorize]
    public class UserDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<IdentityUser> _signInManager;

        public UserDashboardController(ApplicationDbContext context, SignInManager<IdentityUser> signInManager)
        {
            _context = context;
            _signInManager = signInManager;
        }

        public IActionResult Index() => View();

        // --- Attendance Actions ---

        [HttpGet]
        public async Task<IActionResult> DailyLogs()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userLogs = await _context.DailyLogs
                                         .Where(log => log.UserId == userId)
                                         .OrderByDescending(log => log.CheckInTime)
                                         .ToListAsync();
            return View(userLogs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var today = DateTime.Today;

            bool alreadyCheckedIn = await _context.DailyLogs.AnyAsync(log => log.UserId == userId && log.CheckInTime.Date == today);

            if (!alreadyCheckedIn)
            {
                var newLog = new DailyLog { UserId = userId, CheckInTime = DateTime.Now };
                _context.DailyLogs.Add(newLog);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("DailyLogs");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut()
        {
            // SERVER-SIDE VALIDATION: Check if it's after 5:00 PM
            var fivePm = DateTime.Today.AddHours(17); // 5:00 PM
            if (DateTime.Now < fivePm)
            {
                // Add a temporary message to inform the user.
                TempData["ErrorMessage"] = "Check-out is only available after 5:00 PM.";
                return RedirectToAction("DailyLogs");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var today = DateTime.Today;

            var todaysLog = await _context.DailyLogs
                                        .FirstOrDefaultAsync(log => log.UserId == userId && log.CheckInTime.Date == today && log.CheckOutTime == null);

            if (todaysLog != null)
            {
                todaysLog.CheckOutTime = DateTime.Now;
                _context.Update(todaysLog);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("DailyLogs");
        }

        // --- Leave Request Actions ---
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
            if (leaveRequest.EndDate < leaveRequest.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date cannot be before the start date.");
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
            return View(leaveRequest);
        }

        // --- Logout Action ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult>
            PaySlips()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var paySlips = await _context.PaySlips
            .Include(p => p.Payroll) // Eager load the related Payroll data
            .Where(p => p.EmployeeId == userId)
            .OrderByDescending(p => p.Payroll.PayrollMonth)
            .ToListAsync();
            return View(paySlips);
        }

    }
}

