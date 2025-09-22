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
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IncomeApiService _incomeApiService;

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
            var now = DateTime.Now;

            // Use the helper method to get the policy-defined end time
            var scheduledEndTime = GetScheduledEndDateTimeForToday();

            var todaysLog = await _context.DailyLogs.FirstOrDefaultAsync(log => log.UserId == userId && log.CheckInTime.Date == today);

            // This logic now correctly determines if a user can check out
            var attendanceStatus = TodayAttendanceStatus.NotCheckedIn;
            if (todaysLog != null)
            {
                if (todaysLog.CheckOutTime.HasValue)
                {
                    attendanceStatus = TodayAttendanceStatus.Completed;
                }
                else if (now < scheduledEndTime)
                {
                    // If it's before the scheduled end time, set the status to prevent checkout
                    attendanceStatus = TodayAttendanceStatus.CheckedInCannotCheckOut;
                }
                else
                {
                    attendanceStatus = TodayAttendanceStatus.CheckedIn;
                }
            }

            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var upcomingLeave = await _context.LeaveRequests.Where(r => r.RequestingEmployeeId == userId && r.Status == LeaveRequestStatus.Approved && r.StartDate >= today).OrderBy(r => r.StartDate).FirstOrDefaultAsync();
            var pendingLeaveRequestsCount = await _context.LeaveRequests.CountAsync(r => r.RequestingEmployeeId == userId && r.Status == LeaveRequestStatus.Pending);
            var pendingOvertimeRequestsCount = await _context.OvertimeRequests.CountAsync(r => r.RequestingEmployeeId == userId && r.Status == OvertimeStatus.PendingApproval);
            var approvedOvertimeThisMonth = await _context.OvertimeRequests
                .Where(r => r.RequestingEmployeeId == userId && r.Status == OvertimeStatus.Approved && r.OvertimeDate >= startOfMonth && r.OvertimeDate < startOfMonth.AddMonths(1))
                .ToListAsync();
            var totalApprovedHours = approvedOvertimeThisMonth.Sum(r => r.TotalHours);

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var today = DateTime.Today;
            var now = DateTime.Now;

            // Use the helper method for a consistent, secure check on the server
            var scheduledEndDateTime = GetScheduledEndDateTimeForToday();

            if (now < scheduledEndDateTime)
            {
                TempData["ErrorMessage"] = "Check-out is only available after your scheduled end time.";
                return RedirectToAction("DailyLogs");
            }

            var todaysLog = await _context.DailyLogs
                .FirstOrDefaultAsync(log => log.UserId == userId && log.CheckInTime.Date == today && log.CheckOutTime == null);

            if (todaysLog == null)
            {
                TempData["ErrorMessage"] = "No active check-in found to check out from.";
                return RedirectToAction("DailyLogs");
            }

            todaysLog.CheckOutTime = DateTime.Now;

            // ... (Your existing early checkout and overtime logic remains here)

            _context.Update(todaysLog);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "You have successfully checked out.";

            return RedirectToAction("DailyLogs");
        }

        // A private helper method to read the policy and get today's scheduled end time
        private DateTime GetScheduledEndDateTimeForToday()
        {
            var policy = _configuration.GetSection("AttendancePolicy");
            var scheduledEndTimeSpan = TimeSpan.Parse(policy["ScheduledEndTime"]);
            return DateTime.Today.Add(scheduledEndTimeSpan);
        }

        #region Other Actions
        // --- (All other actions like DailyLogs, CheckIn, Leave, Overtime, Sales, etc., remain here) ---

        [HttpGet]
        public async Task<IActionResult> DailyLogs()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var today = DateTime.Today;
            var now = DateTime.Now;

            // Read the policy from appsettings.json
            var policy = _configuration.GetSection("AttendancePolicy");
            var scheduledEndTimeString = policy["ScheduledEndTime"];
            var scheduledEndTime = today.Add(TimeSpan.Parse(scheduledEndTimeString));

            var userLogs = await _context.DailyLogs
                .Where(log => log.UserId == userId)
                .OrderByDescending(log => log.CheckInTime)
                .ToListAsync();

            var todaysLog = userLogs.FirstOrDefault(log => log.CheckInTime.Date == today);

            // THE CHANGE: Use the consolidated UserDashboardViewModel
            var viewModel = new UserDashboardViewModel
            {
                Logs = userLogs,
                HasCheckedInToday = todaysLog != null,
                HasCheckedOutToday = todaysLog?.CheckOutTime.HasValue ?? false,
                CanCheckOutNow = now >= scheduledEndTime,
                ScheduledEndTime = scheduledEndTime.ToString("h:mm tt")
            };

            return View(viewModel);
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

            var policy = _configuration.GetSection("AttendancePolicy");
            var scheduledStartTime = TimeSpan.Parse(policy["ScheduledStartTime"]);
            var gracePeriod = TimeSpan.FromMinutes(int.Parse(policy["GracePeriodMinutes"]));
            var gracePeriodEndTime = today.Add(scheduledStartTime).Add(gracePeriod);

            var newLog = new DailyLog
            {
                UserId = userId,
                CheckInTime = now,
                Status = now > gracePeriodEndTime ? AttendanceStatus.Late : AttendanceStatus.Present
            };

            _context.DailyLogs.Add(newLog);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = newLog.Status == AttendanceStatus.Late ? "You have checked in late." : "You have successfully checked in.";

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
            // 1. Get all products from the external API
            var apiProducts = await _incomeApiService.GetProductsAsync();
            // 2. Get all our local supply records from the database
            var localSupplies = await _context.Supplies.ToDictionaryAsync(s => s.Name, s => s.StockLevel);

            var salesProducts = new List<SalesProductViewModel>();
            foreach (var product in apiProducts)
            {
                // 3. For each product, find its current stock level in our local database
                localSupplies.TryGetValue(product.Title, out var stockLevel);

                salesProducts.Add(new SalesProductViewModel
                {
                    Product = product,
                    StockLevel = stockLevel // This will be 0 if the supply doesn't exist
                });
            }

            var model = new SalesViewModel
            {
                Products = salesProducts,
                CurrentBalance = await _context.CompanyLedger.SumAsync(t => t.Amount)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordSale(string productName, decimal price, DateTime transactionMonth, int quantity)
        {
            // THE FIX: Check for sufficient stock before recording a sale.
            // This assumes product names from the API match supply names in your database.
            var supplyItem = await _context.Supplies.FirstOrDefaultAsync(s => s.Name == productName);

            if (supplyItem == null || supplyItem.StockLevel < quantity)
            {
                TempData["ErrorMessage"] = $"Sale failed: Insufficient stock for {productName}. Current stock: {(supplyItem?.StockLevel ?? 0)}. Please request more supplies.";
                return RedirectToAction("Sales");
            }

            // If stock is sufficient, deduct the quantity and record the sale.
            supplyItem.StockLevel -= quantity;

            var saleTransaction = new CompanyLedger
            {
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                TransactionDate = new DateTime(transactionMonth.Year, transactionMonth.Month, 1),
                Description = $"{quantity} x Sale of {productName}",
                Amount = price * quantity
            };

            _context.CompanyLedger.Add(saleTransaction);

            // Save both the stock update and the new ledger entry to the database.
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Sale of {quantity} x {productName} recorded successfully! Stock updated.";
            return RedirectToAction("Sales");
        }

        [HttpGet]
        public async Task<IActionResult> SupplyCatalog()
        {
            var products = await _incomeApiService.GetProductsAsync();
            return View(products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestSupply(string productName, int quantity)
        {
            if (quantity > 0)
            {
                // Find the supply item in the database by its name.
                var supplyItem = await _context.Supplies.FirstOrDefaultAsync(s => s.Name == productName);
                if (supplyItem == null)
                {
                    TempData["ErrorMessage"] = "Could not request supply: This item does not exist in the local inventory.";
                    return RedirectToAction("Sales");
                }

                // Increase the stock level directly.
                supplyItem.StockLevel += quantity;

                // Create the request record for tracking purposes.
                var request = new SupplyRequest
                {
                    SupplyId = supplyItem.Id,
                    Quantity = quantity,
                    RequestingEmployeeId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    DateRequested = DateTime.Now,
                    Status = SupplyRequestStatus.Approved // Auto-approved for testing
                };

                _context.SupplyRequests.Add(request);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Request approved and {quantity} units of {supplyItem.Name} added to stock.";
            }
            return RedirectToAction("Sales");
        }

        [HttpGet]
        public async Task<IActionResult> MyRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var requests = await _context.SupplyRequests
                .Include(r => r.Supply)
                .Where(r => r.RequestingEmployeeId == userId)
                .OrderByDescending(r => r.DateRequested)
                .ToListAsync();
            return View(requests);
        }

        [HttpGet]
        public async Task<IActionResult> ProductCatalog()
        {
            var products = await _incomeApiService.GetProductsAsync();
            return View(products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestProduct(string productName, decimal price, int quantity)
        {
            if (quantity > 0)
            {
                var request = new ProductRequest
                {
                    RequestingEmployeeId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    ProductName = productName,
                    PricePerUnit = price,
                    Quantity = quantity,
                    DateRequested = DateTime.Now,
                    // THE CHANGE: The status is now set directly to Approved.
                    Status = ProductRequestStatus.Approved
                };
                _context.ProductRequests.Add(request);
                await _context.SaveChangesAsync();
                // You can also update the success message to reflect the change.
                TempData["SuccessMessage"] = "Your product request has been automatically approved.";
            }
            return RedirectToAction("ProductCatalog");
        }

        [HttpGet]
        public async Task<IActionResult> MyProductRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var requests = await _context.ProductRequests
                .Where(r => r.RequestingEmployeeId == userId)
                .OrderByDescending(r => r.DateRequested)
                .ToListAsync();
            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> PaySlips()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var paySlips = await _context.PaySlips
            .Include(p => p.Payroll)
            .Include(p => p.Employee)
            .Where(p => p.EmployeeId == userId)
            .OrderByDescending(p => p.Payroll.PayrollMonth)
            .ToListAsync();
            return View(paySlips);
        }
        #endregion
    }
}

