using IT15.Data;
using IT15.Models;
using IT15.Services;
using IT15.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
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
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAuditService _auditService;

        public UserDashboardController(ApplicationDbContext context, SignInManager<IdentityUser> signInManager, IConfiguration configuration, IncomeApiService incomeApiService, UserManager<IdentityUser> userManager, IAuditService auditService)
        {
            _context = context;
            _signInManager = signInManager;
            _configuration = configuration;
            _incomeApiService = incomeApiService;
            _userManager = userManager;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId) ?? new UserProfile { LeaveBalance = 3 };
            var today = DateTime.Now.Date;

            var todaysLog = await _context.DailyLogs.FirstOrDefaultAsync(log => log.UserId == userId && log.CheckInTime.Date == today);
            var attendanceStatus = GetAttendanceStatus(todaysLog);

            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var upcomingLeave = await _context.LeaveRequests.Where(r => r.RequestingEmployeeId == userId && r.Status == LeaveRequestStatus.Approved && r.StartDate >= today).OrderBy(r => r.StartDate).FirstOrDefaultAsync();
            var pendingLeaveRequestsCount = await _context.LeaveRequests.CountAsync(r => r.RequestingEmployeeId == userId && r.Status == LeaveRequestStatus.Pending);
            var pendingOvertimeRequestsCount = await _context.OvertimeRequests.CountAsync(r => r.RequestingEmployeeId == userId && r.Status == OvertimeStatus.PendingApproval);
            var approvedOvertimeThisMonth = await _context.OvertimeRequests
                .Where(r => r.RequestingEmployeeId == userId && r.Status == OvertimeStatus.Approved && r.OvertimeDate >= startOfMonth && r.OvertimeDate < startOfMonth.AddMonths(1))
                .ToListAsync();
            var totalApprovedHours = approvedOvertimeThisMonth.Sum(r => r.TotalHours);
            var pendingSupplyRequestsCount = await _context.SupplyRequests.CountAsync(r => r.RequestingEmployeeId == userId && r.Status == SupplyRequestStatus.Pending);

            var approvedLeaveThisYear = await _context.LeaveRequests
                .Where(r => r.RequestingEmployeeId == userId && r.Status == LeaveRequestStatus.Approved && r.StartDate.Year == today.Year)
                .ToListAsync();
            int leaveDaysUsed = approvedLeaveThisYear.Sum(r => (r.EndDate - r.StartDate).Days + 1);

            var salesChartLabels = new List<string>();
            var salesChartData = new List<decimal>();
            for (int i = 5; i >= 0; i--)
            {
                var month = today.AddMonths(-i);
                var monthStart = new DateTime(month.Year, month.Month, 1);
                var monthEnd = monthStart.AddMonths(1);
                var monthlySales = await _context.CompanyLedger
                    .Where(s => s.UserId == userId
                                && s.EntryType == LedgerEntryType.Income
                                && s.Category == LedgerEntryCategory.Sales
                                && s.TransactionDate >= monthStart
                                && s.TransactionDate < monthEnd)
                    .SumAsync(s => s.Amount);
                salesChartLabels.Add(monthStart.ToString("MMM yyyy"));
                salesChartData.Add(monthlySales);
            }

            var recentActivity = new List<ActivityLogItem>();
            var recentLogs = await _context.DailyLogs.Where(l => l.UserId == userId).OrderByDescending(l => l.CheckInTime).Take(3).ToListAsync();
            var recentLeaves = await _context.LeaveRequests.Where(l => l.RequestingEmployeeId == userId).OrderByDescending(l => l.DateRequested).Take(3).ToListAsync();
            var recentSales = await _context.CompanyLedger
                .Where(s => s.UserId == userId
                            && s.EntryType == LedgerEntryType.Income
                            && s.Category == LedgerEntryCategory.Sales)
                .OrderByDescending(s => s.TransactionDate)
                .Take(3)
                .ToListAsync();
            recentActivity.AddRange(recentLogs.Select(l => new ActivityLogItem { Timestamp = l.CheckInTime, Description = "Checked in for the day.", Icon = "log-in", Url = Url.Action("DailyLogs") }));
            recentActivity.AddRange(recentLeaves.Select(l => new ActivityLogItem { Timestamp = l.DateRequested, Description = $"Submitted a leave request ({l.Status}).", Icon = "calendar-check", Url = Url.Action("LeaveRequests") }));
            recentActivity.AddRange(recentSales.Select(s => new ActivityLogItem { Timestamp = s.TransactionDate, Description = $"Recorded a sale of {s.Amount:C}.", Icon = "trending-up", Url = Url.Action("SalesHistory") }));

            var model = new UserDashboardViewModel
            {
                AttendanceStatus = attendanceStatus,
                UpcomingLeave = upcomingLeave,
                PendingLeaveRequestsCount = pendingLeaveRequestsCount,
                PendingOvertimeRequestsCount = pendingOvertimeRequestsCount,
                ApprovedOvertimeHoursThisMonth = totalApprovedHours,
                PendingSupplyRequestsCount = pendingSupplyRequestsCount,
                AvailableLeaveDays = userProfile.LeaveBalance,
                LeaveDaysUsed = leaveDaysUsed,
                LeaveDaysRemaining = userProfile.LeaveBalance,
                SalesChartLabels = salesChartLabels,
                SalesChartData = salesChartData,
                RecentActivity = recentActivity.OrderByDescending(a => a.Timestamp).Take(5).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity.Name;
            var today = DateTime.Now.Date;
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
            await _auditService.LogAsync(userId, userName, "User Check-In", $"User '{userName}' checked in.");
            TempData["SuccessMessage"] = "You have successfully checked out.";

            return RedirectToAction("DailyLogs");
        }

        // A private helper method to read the policy and get today's scheduled end time
        private TodayAttendanceStatus GetAttendanceStatus(DailyLog todaysLog)
        {
            if (todaysLog == null) return TodayAttendanceStatus.NotCheckedIn;
            if (todaysLog.CheckOutTime.HasValue) return TodayAttendanceStatus.Completed;

            var scheduledEndTime = GetScheduledEndDateTimeForToday();
            return DateTime.Now < scheduledEndTime ? TodayAttendanceStatus.CheckedInCannotCheckOut : TodayAttendanceStatus.CheckedIn;
        }

        // THE FIX: The duplicate method has been removed. This is the single, correct version.
        private DateTime GetScheduledEndDateTimeForToday()
        {
            var policy = _configuration.GetSection("AttendancePolicy");
            var scheduledEndTimeSpan = TimeSpan.Parse(policy["ScheduledEndTime"]);
            return DateTime.Now.Date.Add(scheduledEndTimeSpan);
        }

        #region Other Actions
        // --- (All other actions like DailyLogs, CheckIn, Leave, Overtime, Sales, etc., remain here) ---

        [HttpGet]
        public async Task<IActionResult> DailyLogs(int? month, int? year, string statusFilter)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var today = DateTime.Now.Date;
            var now = DateTime.Now;

            var policy = _configuration.GetSection("AttendancePolicy");
            var scheduledEndTimeString = policy["ScheduledEndTime"];
            var scheduledEndTime = today.Add(TimeSpan.Parse(scheduledEndTimeString));

            var selectedYear = year ?? today.Year;
            var selectedMonth = month ?? today.Month;

            if (selectedMonth < 1 || selectedMonth > 12)
            {
                selectedMonth = today.Month;
            }

            if (selectedYear < 1)
            {
                selectedYear = today.Year;
            }

            var firstDayOfMonth = new DateTime(selectedYear, selectedMonth, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var statusOptions = new List<string>
            {
                "All",
                "Present",
                "Late",
                "Early Checkout",
                "Leave",
                "Absent",
                "Pending",
                "Weekend",
                "Upcoming"
            };

            var normalizedStatusFilter = string.IsNullOrWhiteSpace(statusFilter) ? "All" : statusFilter.Trim();
            if (!statusOptions.Any(option => option.Equals(normalizedStatusFilter, StringComparison.OrdinalIgnoreCase)))
            {
                normalizedStatusFilter = "All";
            }
            var effectiveStatusFilter = normalizedStatusFilter.Equals("All", StringComparison.OrdinalIgnoreCase)
                ? null
                : normalizedStatusFilter;

            var monthlyLogs = await _context.DailyLogs
                .Where(log => log.UserId == userId && log.CheckInTime.Date >= firstDayOfMonth && log.CheckInTime.Date <= lastDayOfMonth)
                .ToListAsync();

            var todaysLog = await _context.DailyLogs
                .FirstOrDefaultAsync(log => log.UserId == userId && log.CheckInTime.Date == today);

            var availableYears = await _context.DailyLogs
                .Where(log => log.UserId == userId)
                .Select(log => log.CheckInTime.Year)
                .Distinct()
                .ToListAsync();

            var approvedLeaves = await _context.LeaveRequests
                .Where(r => r.RequestingEmployeeId == userId
                            && r.Status == LeaveRequestStatus.Approved
                            && r.EndDate.Date >= firstDayOfMonth
                            && r.StartDate.Date <= lastDayOfMonth)
                .ToListAsync();

            if (!availableYears.Contains(today.Year))
            {
                availableYears.Add(today.Year);
            }

            foreach (var leave in approvedLeaves)
            {
                if (!availableYears.Contains(leave.StartDate.Year))
                {
                    availableYears.Add(leave.StartDate.Year);
                }
                if (!availableYears.Contains(leave.EndDate.Year))
                {
                    availableYears.Add(leave.EndDate.Year);
                }
            }

            if (!availableYears.Contains(selectedYear))
            {
                availableYears.Add(selectedYear);
            }
            availableYears = availableYears.Distinct().OrderBy(y => y).ToList();

            var leaveByDate = new Dictionary<DateTime, LeaveRequest>();
            foreach (var leave in approvedLeaves)
            {
                var rangeStart = leave.StartDate.Date < firstDayOfMonth ? firstDayOfMonth : leave.StartDate.Date;
                var rangeEnd = leave.EndDate.Date > lastDayOfMonth ? lastDayOfMonth : leave.EndDate.Date;

                for (var date = rangeStart; date <= rangeEnd; date = date.AddDays(1))
                {
                    leaveByDate[date] = leave;
                }
            }

            var logsByDate = monthlyLogs
                .GroupBy(log => log.CheckInTime.Date)
                .ToDictionary(group => group.Key, group => group.OrderByDescending(l => l.CheckInTime).First());

            var startOfCalendar = firstDayOfMonth.AddDays(-(int)firstDayOfMonth.DayOfWeek);
            var calendarDays = new List<CalendarDayViewModel>();

            for (int i = 0; i < 42; i++)
            {
                var date = startOfCalendar.AddDays(i);
                var isCurrentMonth = date.Month == selectedMonth && date.Year == selectedYear;
                var isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

                logsByDate.TryGetValue(date.Date, out var logForDay);
                leaveByDate.TryGetValue(date.Date, out var leaveForDay);

                string status;
                string description = string.Empty;

                if (logForDay != null)
                {
                    status = logForDay.Status switch
                    {
                        AttendanceStatus.Present => "Present",
                        AttendanceStatus.Late => "Late",
                        AttendanceStatus.Absent => "Absent",
                        AttendanceStatus.EarlyCheckout => "Early Checkout",
                        _ => "Present"
                    };

                    description = $"Checked in at {logForDay.CheckInTime:hh:mm tt}";
                    if (logForDay.CheckOutTime.HasValue)
                    {
                        description += $", checked out at {logForDay.CheckOutTime.Value:hh:mm tt}";
                    }

                    if (logForDay.Status == AttendanceStatus.Late)
                    {
                        description += " (Late check-in)";
                    }
                    else if (logForDay.Status == AttendanceStatus.EarlyCheckout)
                    {
                        description += " (Early checkout)";
                    }
                }
                else if (leaveForDay != null)
                {
                    status = "Leave";
                    description = $"Approved leave - {leaveForDay.Reason}";
                }
                else if (!isCurrentMonth)
                {
                    status = "Other Month";
                    description = "Outside the selected month.";
                }
                else if (isWeekend)
                {
                    status = "Weekend";
                    description = "Weekend";
                }
                else if (date.Date > today)
                {
                    status = "Upcoming";
                    description = "Scheduled workday";
                }
                else if (date.Date == today)
                {
                    status = todaysLog != null
                        ? (todaysLog.Status == AttendanceStatus.Late ? "Late" :
                            todaysLog.Status == AttendanceStatus.EarlyCheckout ? "Early Checkout" : "Present")
                        : "Pending";

                    if (todaysLog != null)
                    {
                        description = $"Checked in at {todaysLog.CheckInTime:hh:mm tt}";
                        if (todaysLog.CheckOutTime.HasValue)
                        {
                            description += $", checked out at {todaysLog.CheckOutTime.Value:hh:mm tt}";
                        }
                    }
                    else
                    {
                        description = "No check-in yet.";
                    }
                }
                else
                {
                    status = "Absent";
                    description = "No attendance record.";
                }

                var isFilteredOut = !string.IsNullOrEmpty(effectiveStatusFilter) &&
                                    !string.Equals(status, effectiveStatusFilter, StringComparison.OrdinalIgnoreCase);

                if (!isCurrentMonth)
                {
                    isFilteredOut = true;
                }

                calendarDays.Add(new CalendarDayViewModel
                {
                    Date = date,
                    IsCurrentMonth = isCurrentMonth,
                    IsToday = date.Date == today,
                    IsWeekend = isWeekend,
                    IsLeave = leaveForDay != null,
                    Status = status,
                    StatusDescription = description,
                    Log = logForDay,
                    IsFilteredOut = isFilteredOut
                });
            }

            var viewModel = new UserDashboardViewModel
            {
                Logs = monthlyLogs.OrderByDescending(log => log.CheckInTime).ToList(),
                HasCheckedInToday = todaysLog != null,
                HasCheckedOutToday = todaysLog?.CheckOutTime.HasValue ?? false,
                CanCheckOutNow = now >= scheduledEndTime,
                ScheduledEndTime = scheduledEndTime.ToString("h:mm tt"),
                SelectedMonth = selectedMonth,
                SelectedYear = selectedYear,
                SelectedStatusFilter = normalizedStatusFilter,
                AvailableYears = availableYears,
                AvailableStatusFilters = statusOptions,
                CalendarDays = calendarDays
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity.Name;
            var today = DateTime.Now.Date;
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
            await _auditService.LogAsync(userId, userName, "User Check-In", $"User '{userName}' checked in.");
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
        public async Task<IActionResult> ApplyForLeave()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

            // If a user doesn't have a profile yet (e.g., they are a new user),
            // create one for them with the default leave balance.
            if (userProfile == null)
            {
                userProfile = new UserProfile { UserId = userId, LeaveBalance = 3 };
                _context.UserProfiles.Add(userProfile);
                await _context.SaveChangesAsync();
            }

            var model = new UserDashboardViewModel
            {
                LeaveRequest = new LeaveRequest { StartDate = DateTime.Now.Date, EndDate = DateTime.Now.Date
        },
                AvailableLeaveDays = userProfile.LeaveBalance
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // THE FIX: Change the parameter to bind specifically to the "LeaveRequest" form data.
        public async Task<IActionResult> ApplyForLeave([Bind(Prefix = "LeaveRequest")] LeaveRequest leaveRequest)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            int availableDays = userProfile?.LeaveBalance ?? 0;

            var requestedDays = (leaveRequest.EndDate - leaveRequest.StartDate).Days + 1;

            if (requestedDays <= 0)
            {
                ModelState.AddModelError("LeaveRequest.EndDate", "End date must be on or after the start date.");
            }
            if (requestedDays > availableDays)
            {
                ModelState.AddModelError("LeaveRequest.EndDate", $"You cannot request {requestedDays} days. You only have {availableDays} leave days available.");
            }

            if (ModelState.IsValid)
            {
                leaveRequest.RequestingEmployeeId = userId;
                leaveRequest.DateRequested = DateTime.Now;
                leaveRequest.Status = LeaveRequestStatus.Pending;

                _context.Add(leaveRequest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(LeaveRequests));
            }

            // If validation fails, we rebuild the full ViewModel to redisplay the page correctly.
            var model = new UserDashboardViewModel
            {
                LeaveRequest = leaveRequest,
                AvailableLeaveDays = availableDays
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Sales()
        {
            var apiProducts = await _incomeApiService.GetProductsAsync();
            var localSupplies = await _context.Supplies.ToDictionaryAsync(s => s.Name, s => s.StockLevel);

            var salesProducts = apiProducts.Select(product => new SalesProductViewModel
            {
                Product = product,
                StockLevel = localSupplies.TryGetValue(product.Title, out var stockLevel) ? stockLevel : 0
            }).ToList();

            // --- NEW: Fetch additional data for the view ---
            var salesPolicy = _configuration.GetSection("SalesPolicy");
            var revenueMargin = decimal.Parse(salesPolicy["RevenueMarginPercent"]);

            var recentSales = await _context.CompanyLedger
                .Include(s => s.User) // Eager load the user who made the sale
                .OrderByDescending(s => s.TransactionDate)
                .ThenByDescending(s => s.Id)
                .Take(5)
                .ToListAsync();

            var model = new SalesViewModel
            {
                Products = salesProducts,
                CurrentBalance = await _context.CompanyLedger.SumAsync(t => t.Amount),
                RevenueMarginPercent = revenueMargin,
                RecentSales = recentSales
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordSale(string productName, decimal price, int quantity)
        {
            // Find the supply item in your local database to get its cost and stock level.
            // The 'Cost' property was populated by your SeedData process.
            var supplyItem = await _context.Supplies.FirstOrDefaultAsync(s => s.Name == productName);

            if (supplyItem == null || supplyItem.StockLevel < quantity)
            {
                TempData["ErrorMessage"] = $"Sale failed: Insufficient stock for {productName}.";
                return RedirectToAction("Sales");
            }

            // --- MODIFICATION START ---

            // 1. Calculate the total base cost for the items sold.
            //    This uses the 'Cost' property from your Supply entity.
            var baseCost = supplyItem.Cost * quantity;

            // 2. Keep your existing revenue calculation exactly as it was.
            var salesPolicy = _configuration.GetSection("SalesPolicy");
            var revenueMargin = decimal.Parse(salesPolicy["RevenueMarginPercent"]) / 100m;
            var totalSaleAmount = price * quantity;
            var revenueAmount = totalSaleAmount * revenueMargin;

            // 3. Combine the base cost and the calculated revenue for the final ledger amount.
            var finalTransactionAmount = baseCost + revenueAmount;

            // --- MODIFICATION END ---

            // Update stock level as before
            supplyItem.StockLevel -= quantity;

            // Create the ledger entry with the new combined amount
            var reference = $"SL-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant()}";
            var saleTransaction = new CompanyLedger
            {
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                TransactionDate = DateTime.Now,
                Description = $"{quantity} x Sale of {productName}",
                EntryType = LedgerEntryType.Income,
                Category = LedgerEntryCategory.Sales,
                ReferenceNumber = reference,
                Counterparty = User.Identity?.Name ?? "Internal",
                Amount = finalTransactionAmount // Use the new combined amount here
            };

            _context.CompanyLedger.Add(saleTransaction);
            await _context.SaveChangesAsync();

            // Log the audit and redirect as before
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity.Name;
            await _auditService.LogAsync(userId, userName, "Sale Recorded", $"User '{userName}' recorded a sale of {quantity} x {productName}.");

            TempData["SuccessMessage"] = $"Sale recorded successfully! Stock updated.";
            return RedirectToAction("Sales");
        }

        [HttpGet]
        public async Task<IActionResult> SalesHistory(string filterType, DateTime? startDate, DateTime? endDate, string employeeId)
        {
            var today = DateTime.Now.Date;
            if (filterType == "daily")
            {
                startDate = today;
                endDate = today;
            }
            else if (filterType == "monthly")
            {
                startDate = new DateTime(today.Year, today.Month, 1);
                endDate = startDate.Value.AddMonths(1).AddDays(-1);
            }

            IQueryable<CompanyLedger> salesQuery = _context.CompanyLedger
                .Include(s => s.User)
                .Where(s => s.EntryType == LedgerEntryType.Income && s.Category == LedgerEntryCategory.Sales);

            if (startDate.HasValue)
            {
                salesQuery = salesQuery.Where(s => s.TransactionDate.Date >= startDate.Value.Date);
            }
            if (endDate.HasValue)
            {
                salesQuery = salesQuery.Where(s => s.TransactionDate.Date <= endDate.Value.Date);
            }
            if (!string.IsNullOrEmpty(employeeId))
            {
                salesQuery = salesQuery.Where(s => s.UserId == employeeId);
            }

            var salesRecords = await salesQuery.OrderByDescending(s => s.TransactionDate).ToListAsync();

            // --- NEW: Data Aggregation for Charts ---
            var salesOverTime = salesRecords
                .GroupBy(s => s.TransactionDate.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(s => s.Amount) })
                .OrderBy(x => x.Date);

            var salesByEmployee = salesRecords
                .GroupBy(s => s.User?.UserName ?? "N/A")
                .Select(g => new { Employee = g.Key, Total = g.Sum(s => s.Amount) })
                .OrderByDescending(x => x.Total);

            var employees = await _userManager.Users.Select(u => new SelectListItem { Value = u.Id, Text = u.UserName }).ToListAsync();

            var model = new SalesHistoryViewModel
            {
                SalesRecords = salesRecords,
                Employees = employees,
                StartDate = startDate,
                EndDate = endDate,
                SelectedEmployeeId = employeeId,
                FilterType = filterType,
                TotalRevenueForPeriod = salesRecords.Sum(s => s.Amount),
                SalesOverTimeLabels = salesOverTime.Select(x => x.Date.ToString("MMM dd")).ToList(),
                SalesOverTimeData = salesOverTime.Select(x => x.Total).ToList(),
                SalesByEmployeeLabels = salesByEmployee.Select(x => x.Employee).ToList(),
                SalesByEmployeeData = salesByEmployee.Select(x => x.Total).ToList()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> SupplyCatalog()
        {
            // THE FIX: We now explicitly format the currency using the correct culture.
            var culture = new CultureInfo("en-PH");
            var deliveryServices = await _context.DeliveryServices
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = $"{d.Name} - {d.Fee.ToString("C", culture)}"
                }).ToListAsync();

            var model = new SupplyCatalogViewModel
            {
                Products = await _incomeApiService.GetProductsAsync(),
                DeliveryServices = deliveryServices
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestSupply(string productName, int quantity, int deliveryServiceId)
        {
            if (quantity > 0)
            {
                var supply = await _context.Supplies.Include(s => s.Supplier).FirstOrDefaultAsync(s => s.Name == productName);
                var delivery = await _context.DeliveryServices.FindAsync(deliveryServiceId);
                var currentUser = await _userManager.GetUserAsync(User);

                if (supply != null && delivery != null)
                {
                    var request = new SupplyRequest
                    {
                        SupplyId = supply.Id,
                        Quantity = quantity,
                        DeliveryServiceId = deliveryServiceId,
                        TotalCost = (supply.Cost * quantity) + delivery.Fee,
                        RequestingEmployeeId = currentUser.Id,
                        DateRequested = DateTime.Now,
                        Status = SupplyRequestStatus.Pending
                    };

                    _context.SupplyRequests.Add(request);
                    await _context.SaveChangesAsync();

                    await _auditService.LogAsync(currentUser.Id, currentUser.UserName, "Supply Request Submitted", $"User '{currentUser.UserName}' submitted a supply request for {quantity} of '{supply.Name}' that is pending admin approval.");
                    TempData["SuccessMessage"] = "Supply request submitted for admin approval.";
                }
                else
                {
                    // THE FIX: Add an explicit error message when the supply is not found.
                    TempData["ErrorMessage"] = "Could not request supply: This product does not exist in the local inventory. Please ensure the database has been seeded correctly.";
                }
            }
            return RedirectToAction("SupplyCatalog");
        }

        [HttpGet]
        public async Task<IActionResult> MyRequests()
        {
            var requests = await _context.SupplyRequests
                .Include(r => r.Supply)
                    .ThenInclude(s => s.Supplier)
                .Include(r => r.DeliveryService)
                .Include(r => r.RequestingEmployee) // Eager load the employee who made the request
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
        public async Task<IActionResult> Resignation()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingRequest = await _context.ResignationRequests
                .FirstOrDefaultAsync(r => r.RequestingEmployeeId == userId && r.Status != ResignationStatus.Denied);

            var model = new ResignationViewModel
            {
                ExistingRequest = existingRequest,
                NewRequest = new ResignationRequest() // Initialize for the form
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // THE FIX: Change the parameter to bind specifically to the "NewRequest" part of the form.
        public async Task<IActionResult> SubmitResignation([Bind(Prefix = "NewRequest")] ResignationRequest resignationRequest)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity.Name;

            var hasActiveRequest = await _context.ResignationRequests
                .AnyAsync(r => r.RequestingEmployeeId == userId && r.Status != ResignationStatus.Denied);

            if (hasActiveRequest)
            {
                ModelState.AddModelError(string.Empty, "You already have an active resignation request.");
            }

            // The model state will now correctly validate ONLY the fields from the form.
            if (ModelState.IsValid)
            {
                resignationRequest.RequestingEmployeeId = userId;
                resignationRequest.DateSubmitted = DateTime.Now;
                resignationRequest.Status = ResignationStatus.Pending;

                _context.ResignationRequests.Add(resignationRequest);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(userId, userName, "Resignation Submitted", $"User '{userName}' submitted a resignation request.");
                TempData["SuccessMessage"] = "Your resignation request has been successfully submitted.";
                return RedirectToAction("Resignation");
            }

            // If validation fails, we need to rebuild the full view model to redisplay the page correctly.
            var fullViewModel = new ResignationViewModel
            {
                ExistingRequest = hasActiveRequest ? await _context.ResignationRequests.FirstOrDefaultAsync(r => r.RequestingEmployeeId == userId) : null,
                NewRequest = resignationRequest // Pass back the invalid model to show error messages
            };
            return View("Resignation", fullViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> PaySlips(string search, DateTime? startDate, DateTime? endDate)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            ViewData["Search"] = search;
            ViewData["StartDate"] = startDate?.ToString("yyyy-MM");
            ViewData["EndDate"] = endDate?.ToString("yyyy-MM");

            var query = _context.PaySlips
                .Include(p => p.Payroll)
                .Include(p => p.Employee)
                .Where(p => p.EmployeeId == userId);

            if (startDate.HasValue)
            {
                var start = new DateTime(startDate.Value.Year, startDate.Value.Month, 1);
                query = query.Where(p => p.Payroll.PayrollMonth >= start);
            }

            if (endDate.HasValue)
            {
                var end = new DateTime(endDate.Value.Year, endDate.Value.Month, 1).AddMonths(1);
                query = query.Where(p => p.Payroll.PayrollMonth < end);
            }

            var paySlips = await query
                .OrderByDescending(p => p.Payroll.PayrollMonth)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                paySlips = paySlips
                    .Where(p =>
                    {
                        var period = p.Payroll?.PayrollMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
                        var userName = p.Employee?.UserName ?? string.Empty;
                        var email = p.Employee?.Email ?? string.Empty;
                        return period.Contains(term, StringComparison.OrdinalIgnoreCase)
                               || userName.Contains(term, StringComparison.OrdinalIgnoreCase)
                               || email.Contains(term, StringComparison.OrdinalIgnoreCase);
                    })
                    .ToList();
            }

            return View(paySlips);
        }
        #endregion
    }
}
