using IT15.Data;
using IT15.Models;
using IT15.Services;
using IT15.ViewModels;
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
        // THE FIX: Change ApplicationUser back to the default IdentityUser
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IncomeApiService _incomeApiService;

        // THE FIX: The constructor must also request the default SignInManager
        public UserDashboardController(ApplicationDbContext context, SignInManager<IdentityUser> signInManager, IConfiguration configuration, IncomeApiService incomeApiService)
        {
            _context = context;
            _signInManager = signInManager;
            _configuration = configuration;
            _incomeApiService = incomeApiService;
        }

        [HttpGet]
       
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            // Fetch all necessary data
            var todaysLog = await _context.DailyLogs.FirstOrDefaultAsync(log => log.UserId == userId && log.CheckInTime.Date == today);
            var attendanceStatus = TodayAttendanceStatus.NotCheckedIn;
            if (todaysLog != null)
            {
                attendanceStatus = todaysLog.CheckOutTime.HasValue ? TodayAttendanceStatus.Completed : TodayAttendanceStatus.CheckedIn;
            }
            var upcomingLeave = await _context.LeaveRequests.Where(r => r.RequestingEmployeeId == userId && r.Status == LeaveRequestStatus.Approved && r.StartDate >= today).OrderBy(r => r.StartDate).FirstOrDefaultAsync();
            var pendingLeaveRequestsCount = await _context.LeaveRequests.CountAsync(r => r.RequestingEmployeeId == userId && r.Status == LeaveRequestStatus.Pending);
            var pendingOvertimeRequestsCount = await _context.OvertimeRequests.CountAsync(r => r.RequestingEmployeeId == userId && r.Status == OvertimeStatus.PendingApproval);
            var approvedOvertimeThisMonth = await _context.OvertimeRequests
                .Where(r => r.RequestingEmployeeId == userId &&
                             r.Status == OvertimeStatus.Approved &&
                             r.OvertimeDate >= startOfMonth &&
                             r.OvertimeDate < startOfMonth.AddMonths(1))
                .ToListAsync();
            var totalApprovedHours = approvedOvertimeThisMonth.Sum(r => r.TotalHours);

            // THE FIX: Ensure all fetched data is assigned to the ViewModel.
            var model = new UserDashboardViewModel
            {
                AttendanceStatus = attendanceStatus,
                UpcomingLeave = upcomingLeave,
                PendingLeaveRequestsCount = pendingLeaveRequestsCount,
                PendingOvertimeRequestsCount = pendingOvertimeRequestsCount,
                ApprovedOvertimeHoursThisMonth = totalApprovedHours
            };

            return View(model);
        }

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
            var now = DateTime.Now;

            bool alreadyCheckedIn = await _context.DailyLogs.AnyAsync(log => log.UserId == userId && log.CheckInTime.Date == today);
            if (alreadyCheckedIn)
            {
                TempData["ErrorMessage"] = "You have already checked in today.";
                return RedirectToAction("DailyLogs");
            }

            // Read policies from appsettings.json
            var policy = _configuration.GetSection("AttendancePolicy");
            var scheduledStartTime = TimeSpan.Parse(policy["ScheduledStartTime"]);
            var gracePeriod = TimeSpan.FromMinutes(int.Parse(policy["GracePeriodMinutes"]));
            var gracePeriodEndTime = today.Add(scheduledStartTime).Add(gracePeriod);

            var newLog = new DailyLog
            {
                UserId = userId,
                CheckInTime = now,
                // Determine if the check-in is late
                Status = now > gracePeriodEndTime ? AttendanceStatus.Late : AttendanceStatus.Present
            };

            _context.DailyLogs.Add(newLog);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = newLog.Status == AttendanceStatus.Late ? "You have checked in late." : "You have successfully checked in.";

            return RedirectToAction("DailyLogs");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var today = DateTime.Today;

            var todaysLog = await _context.DailyLogs
                .FirstOrDefaultAsync(log => log.UserId == userId && log.CheckInTime.Date == today && log.CheckOutTime == null);

            if (todaysLog == null)
            {
                TempData["ErrorMessage"] = "No active check-in found to check out from.";
                return RedirectToAction("DailyLogs");
            }

            todaysLog.CheckOutTime = DateTime.Now;

            // Business logic for early checkout can still apply if needed
            var policy = _configuration.GetSection("AttendancePolicy");
            var minHours = double.Parse(policy["MinimumHoursForFullDay"]);
            var scheduledEndDateTime = today.Add(TimeSpan.Parse(policy["ScheduledEndTime"]));
            if (DateTime.Now < scheduledEndDateTime && (todaysLog.CheckOutTime.Value - todaysLog.CheckInTime).TotalHours < minHours)
            {
                todaysLog.Status = AttendanceStatus.EarlyCheckout;
            }

            _context.Update(todaysLog);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "You have successfully checked out.";

            return RedirectToAction("DailyLogs");
        }

        [HttpGet]
        public async Task<IActionResult> OvertimeRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var requests = await _context.OvertimeRequests
                .Where(r => r.RequestingEmployeeId == userId)
                .OrderByDescending(r => r.DateRequested)
                .ToListAsync();
            return View(requests);
        }

        [HttpGet]
        public IActionResult ApplyForOvertime()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyForOvertime(OvertimeRequest overtimeRequest)
        {
            if (overtimeRequest.EndTime <= overtimeRequest.StartTime)
            {
                ModelState.AddModelError("EndTime", "End time must be after start time.");
            }

            if (ModelState.IsValid)
            {
                overtimeRequest.RequestingEmployeeId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                overtimeRequest.DateRequested = DateTime.Now;
                overtimeRequest.Status = OvertimeStatus.PendingApproval;
                _context.Add(overtimeRequest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(OvertimeRequests));
            }
            return View(overtimeRequest);
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

        [HttpGet]
        public async Task<IActionResult> Sales()
        {
            ViewData["CurrentBalance"] = await _context.CompanyLedger.SumAsync(t => t.Amount);
            var products = await _incomeApiService.GetProductsAsync();
            return View(products);
        }

        // POST: /UserDashboard/RecordSale
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordSale(string productName, decimal price, DateTime transactionMonth)
        {
            if (price > 0)
            {
                var saleTransaction = new CompanyLedger
                {
                    UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    TransactionDate = new DateTime(transactionMonth.Year, transactionMonth.Month, 1),
                    Description = $"Sale of {productName}",
                    Amount = price
                };
                _context.CompanyLedger.Add(saleTransaction);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Sale for {transactionMonth:MMMM yyyy} recorded successfully!";
            }
            return RedirectToAction("Sales"); // Redirect back to the Sales page
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
            .Include(p => p.Employee)
            .Where(p => p.EmployeeId == userId)
            .OrderByDescending(p => p.Payroll.PayrollMonth)
            .ToListAsync();
            return View(paySlips);
        }

    }
}

