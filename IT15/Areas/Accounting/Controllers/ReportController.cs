using IT15.Data;
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

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Accounting/Report
        public async Task<IActionResult> Index(DateTime? reportMonth)
        {
            var monthToQuery = reportMonth ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var startOfMonth = new DateTime(monthToQuery.Year, monthToQuery.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            // Fetch all transactions for the selected month, with user details
            var transactions = await _context.CompanyLedger
                .Include(t => t.User)
                .Where(t => t.TransactionDate >= startOfMonth && t.TransactionDate < endOfMonth)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            // Calculate the detailed financial summary by categorizing each transaction
            var viewModel = new FinancialReportViewModel
            {
                ReportMonth = startOfMonth,
                TotalRevenue = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount),
                TotalPayrollExpense = transactions.Where(t => t.Description.StartsWith("Payroll expense")).Sum(t => t.Amount),
                TotalDeliveryFeeExpense = transactions.Where(t => t.Description.StartsWith("Delivery Fee")).Sum(t => t.Amount),
                TotalOperationalExpense = transactions.Where(t => t.Description.StartsWith("Operational Cost")).Sum(t => t.Amount),
                Transactions = transactions // Pass the full list for the detailed log
            };

            return View(viewModel);
        }
    }
}

