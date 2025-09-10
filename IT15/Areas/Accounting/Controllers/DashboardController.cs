using IT15.Data;
using IT15.Models;
using IT15.ViewModels.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IT15.Areas.Accounting.Controllers
{
    [Area("Accounting")]
    [Authorize(Roles = "Admin,Accounting")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.UtcNow;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            var pendingPayrolls = await _context.Payrolls
                .Where(p => p.Status == PayrollStatus.PendingApproval)
                .Include(p => p.PaySlips)
                .ToListAsync();

            var viewModel = new AccountingDashboardViewModel
            {
                PayrollsPendingApprovalCount = pendingPayrolls.Count,
                TotalPendingPayrollValue = pendingPayrolls.SelectMany(p => p.PaySlips).Sum(s => s.NetPay),
                PayrollsApprovedThisMonth = await _context.Payrolls
                    .CountAsync(p => p.Status != PayrollStatus.PendingApproval && p.DateApproved.HasValue && p.DateApproved.Value.Month == today.Month)
            };

            return View(viewModel);
        }
    }
}
