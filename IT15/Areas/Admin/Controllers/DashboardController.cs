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

            var viewModel = new DashboardViewModel
            {
                TotalUsers = await _userManager.Users.CountAsync(),

                AttendanceTodayCount = await _context.DailyLogs
                    .CountAsync(log => log.CheckInTime.Date == today),

                UsersOnLeaveToday = await _context.LeaveRequests
                    .CountAsync(req => req.Status == LeaveRequestStatus.Approved &&
                                       today >= req.StartDate.Date &&
                                       today <= req.EndDate.Date),

                PendingSupplyRequests = await _context.SupplyRequests
                    .CountAsync(r => r.Status == SupplyRequestStatus.Pending),

                PendingLeaveRequests = await _context.LeaveRequests
                    .CountAsync(r => r.Status == LeaveRequestStatus.Pending),

                PendingOvertimeRequests = await _context.OvertimeRequests
                    .CountAsync(r => r.Status == OvertimeStatus.PendingApproval),

                SalesThisMonth = await _context.CompanyLedger
                    .Where(t => t.EntryType == LedgerEntryType.Income &&
                                t.Category == LedgerEntryCategory.Sales &&
                                t.TransactionDate >= startOfMonth)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0m,

                ExpensesThisMonth = await _context.CompanyLedger
                    .Where(t => t.EntryType == LedgerEntryType.Expense &&
                                t.TransactionDate >= startOfMonth)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0m
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

