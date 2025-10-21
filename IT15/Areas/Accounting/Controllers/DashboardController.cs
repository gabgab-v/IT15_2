using IT15.Data;
using IT15.Models;
using IT15.Services;
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
        private readonly FinancialAnalyticsService _analyticsService;

        public DashboardController(ApplicationDbContext context, FinancialAnalyticsService analyticsService)
        {
            _context = context;
            _analyticsService = analyticsService;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.UtcNow;
            var pendingPayrolls = await _context.Payrolls
                .Where(p => p.Status == PayrollStatus.PendingApproval)
                .Include(p => p.PaySlips)
                .ToListAsync();

            var snapshot = await _analyticsService.GetSnapshotAsync(today);
            var receivablesSummary = await _analyticsService.GetReceivablesSummaryAsync(today);
            var payablesSummary = await _analyticsService.GetPayablesSummaryAsync(today);
            var revenueAnalysis = await _analyticsService.GetRevenueAnalysisAsync(12);

            var viewModel = new AccountingDashboardViewModel
            {
                PayrollsPendingApprovalCount = pendingPayrolls.Count,
                TotalPendingPayrollValue = pendingPayrolls.SelectMany(p => p.PaySlips).Sum(s => s.NetPay),
                PayrollsApprovedThisMonth = await _context.Payrolls
                    .CountAsync(p => p.Status != PayrollStatus.PendingApproval && p.DateApproved.HasValue && p.DateApproved.Value.Month == today.Month),
                AvailableFunds = await _context.CompanyLedger.SumAsync(t => t.Amount),
                FinancialSnapshot = snapshot,
                ReceivablesSummary = receivablesSummary,
                PayablesSummary = payablesSummary,
                RevenueAnalysis = revenueAnalysis
            };

            return View(viewModel);
        }
    }
}
