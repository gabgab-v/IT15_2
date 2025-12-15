using IT15.Data;
using IT15.Models;
using IT15.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IT15.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public DashboardController(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.UtcNow.Date;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var attendanceWindowStart = today.AddDays(-6);
            var financialRangeStart = startOfMonth.AddMonths(-5);

            var totalUsers = await _userManager.Users.CountAsync();

            var attendanceTodayCount = await _context.DailyLogs
                .CountAsync(log => log.CheckInTime.Date == today);

            var usersOnLeaveToday = await _context.LeaveRequests
                .CountAsync(req => req.Status == LeaveRequestStatus.Approved &&
                                   today >= req.StartDate.Date &&
                                   today <= req.EndDate.Date);

            var pendingSupplyRequests = await _context.SupplyRequests
                .CountAsync(r => r.Status == SupplyRequestStatus.Pending);

            var pendingOvertimeRequests = await _context.OvertimeRequests
                .CountAsync(r => r.Status == OvertimeStatus.PendingApproval);

            var leaveStatusCounts = await _context.LeaveRequests
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Status, g => g.Count);

            var pendingLeaveRequests = leaveStatusCounts.TryGetValue(LeaveRequestStatus.Pending, out var pendingLeaves)
                ? pendingLeaves
                : 0;
            var approvedLeaveRequests = leaveStatusCounts.TryGetValue(LeaveRequestStatus.Approved, out var approvedLeaves)
                ? approvedLeaves
                : 0;
            var deniedLeaveRequests = leaveStatusCounts.TryGetValue(LeaveRequestStatus.Denied, out var deniedLeaves)
                ? deniedLeaves
                : 0;

            var salesThisMonth = await _context.CompanyLedger
                .Where(t => t.EntryType == LedgerEntryType.Income &&
                            t.Category == LedgerEntryCategory.Sales &&
                            t.TransactionDate >= startOfMonth)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            var expensesThisMonth = await _context.CompanyLedger
                .Where(t => t.EntryType == LedgerEntryType.Expense &&
                            t.TransactionDate >= startOfMonth)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            var ledgerBuckets = await _context.CompanyLedger
                .Where(t => t.TransactionDate >= financialRangeStart)
                .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month, t.EntryType })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    g.Key.EntryType,
                    Total = g.Sum(x => x.Amount)
                })
                .ToListAsync();

            var financeLabels = new List<string>();
            var incomeSeries = new List<decimal>();
            var expenseSeries = new List<decimal>();

            for (var monthStart = financialRangeStart; monthStart <= startOfMonth; monthStart = monthStart.AddMonths(1))
            {
                financeLabels.Add(monthStart.ToString("MMM yy"));
                incomeSeries.Add(ledgerBuckets
                    .Where(x => x.Year == monthStart.Year && x.Month == monthStart.Month && x.EntryType == LedgerEntryType.Income)
                    .Sum(x => x.Total));
                expenseSeries.Add(ledgerBuckets
                    .Where(x => x.Year == monthStart.Year && x.Month == monthStart.Month && x.EntryType == LedgerEntryType.Expense)
                    .Sum(x => x.Total));
            }

            var attendanceByDay = await _context.DailyLogs
                .Where(log => log.CheckInTime.Date >= attendanceWindowStart && log.CheckInTime.Date <= today)
                .GroupBy(log => log.CheckInTime.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Date, x => x.Count);

            var attendanceLabels = new List<string>();
            var attendanceData = new List<int>();

            for (var date = attendanceWindowStart; date <= today; date = date.AddDays(1))
            {
                attendanceLabels.Add(date.ToString("MMM dd"));
                attendanceData.Add(attendanceByDay.TryGetValue(date, out var count) ? count : 0);
            }

            var viewModel = new DashboardViewModel
            {
                TotalUsers = totalUsers,
                AttendanceTodayCount = attendanceTodayCount,
                UsersOnLeaveToday = usersOnLeaveToday,
                PendingSupplyRequests = pendingSupplyRequests,
                PendingLeaveRequests = pendingLeaveRequests,
                PendingOvertimeRequests = pendingOvertimeRequests,
                SalesThisMonth = salesThisMonth,
                ExpensesThisMonth = expensesThisMonth,
                FinanceChartLabels = financeLabels,
                FinanceIncomeSeries = incomeSeries,
                FinanceExpenseSeries = expenseSeries,
                AttendanceChartLabels = attendanceLabels,
                AttendanceChartData = attendanceData,
                LeaveStatusLabels = new List<string> { "Pending", "Approved", "Denied" },
                LeaveStatusData = new List<int> { pendingLeaveRequests, approvedLeaveRequests, deniedLeaveRequests },
                ApprovalBacklogLabels = new List<string> { "Supply", "Leave", "Overtime" },
                ApprovalBacklogData = new List<int> { pendingSupplyRequests, pendingLeaveRequests, pendingOvertimeRequests }
            };

            viewModel.RecentAuditLogs = await _context.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(6)
                .Select(a => new RecentAuditLog
                {
                    Timestamp = a.Timestamp.ToString("MMM dd, HH:mm"),
                    UserName = string.IsNullOrEmpty(a.UserName) ? "System" : a.UserName,
                    ActionType = a.ActionType,
                    Details = a.Details
                })
                .ToListAsync();

            return View(viewModel);
        }
    }
}

