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
    [Authorize(Roles = "Accounting,Admin")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly FinancialAnalyticsService _analyticsService;

        public ReportController(ApplicationDbContext context, FinancialAnalyticsService analyticsService)
        {
            _context = context;
            _analyticsService = analyticsService;
        }

        // GET: /Accounting/Report
        public async Task<IActionResult> Index(DateTime? reportMonth)
        {
            var monthToQuery = reportMonth ?? new DateTime(DateTime.UtcNow.Date.Year, DateTime.UtcNow.Date.Month, 1);
            var startOfMonth = new DateTime(monthToQuery.Year, monthToQuery.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            var transactions = await _context.CompanyLedger
                .Include(t => t.User)
                .Where(t => t.TransactionDate >= startOfMonth && t.TransactionDate < endOfMonth)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            var totalRevenue = transactions
                .Where(t => t.EntryType == LedgerEntryType.Income)
                .Sum(t => t.Amount);

            var payrollExpense = transactions
                .Where(t => t.EntryType == LedgerEntryType.Expense && t.Category == LedgerEntryCategory.Payroll)
                .Sum(t => -t.Amount);

            var supplyExpense = transactions
                .Where(t => t.EntryType == LedgerEntryType.Expense && t.Category == LedgerEntryCategory.Supplies)
                .Sum(t => -t.Amount);

            var operationalEntries = transactions
                .Where(t => t.EntryType == LedgerEntryType.Expense && t.Category == LedgerEntryCategory.Operations)
                .ToList();

            var deliveryExpense = operationalEntries
                .Where(t => t.Description.Contains("Delivery", StringComparison.OrdinalIgnoreCase))
                .Sum(t => -t.Amount);

            var otherOperationalExpense = operationalEntries.Sum(t => -t.Amount) - deliveryExpense;
            if (otherOperationalExpense < 0) otherOperationalExpense = 0;

            var receivablesSummary = await _analyticsService.GetReceivablesSummaryAsync(DateTime.UtcNow);
            var payablesSummary = await _analyticsService.GetPayablesSummaryAsync(DateTime.UtcNow);
            var revenueAnalysis = await _analyticsService.GetRevenueAnalysisAsync(12);

            var viewModel = new FinancialReportViewModel
            {
                ReportMonth = startOfMonth,
                TotalRevenue = totalRevenue,
                TotalPayrollExpense = payrollExpense,
                TotalSupplyExpense = supplyExpense,
                TotalDeliveryFeeExpense = deliveryExpense,
                TotalOperationalExpense = otherOperationalExpense,
                Transactions = transactions,
                ReceivablesSummary = receivablesSummary,
                PayablesSummary = payablesSummary,
                RevenueAnalysis = revenueAnalysis
            };

            return View(viewModel);
        }
    }
}
