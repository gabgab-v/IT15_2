using IT15.Data;
using IT15.Models;
using IT15.ViewModels.Accounting; // Add this using
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic; // Add this using
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IT15.Areas.Accounting.Controllers
{
    [Area("Accounting")]
    [Authorize(Roles = "Accounting,Admin")]
    public class AccountingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AccountingController> _logger;

        public AccountingController(ApplicationDbContext context, UserManager<IdentityUser> userManager, ILogger<AccountingController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var pendingPayrolls = await _context.Payrolls
                .Include(p => p.PaySlips)
                .Where(p => p.Status == PayrollStatus.PendingApproval && !p.IsArchived)
                .OrderByDescending(p => p.PayrollMonth)
                .ToListAsync();

            var approvalViewModels = new List<PayrollApprovalViewModel>();

            foreach (var payroll in pendingPayrolls)
            {
                // Calculate the available funds UP TO the end of the payroll month.
                var endOfMonth = payroll.PayrollMonth.AddMonths(1).AddDays(-1);
                var fundsForPeriod = await _context.CompanyLedger
                    .Where(t => t.TransactionDate <= endOfMonth)
                    .SumAsync(t => t.Amount);

                approvalViewModels.Add(new PayrollApprovalViewModel
                {
                    Payroll = payroll,
                    AvailableFundsForPeriod = fundsForPeriod
                });
            }

            // Pass the current total balance to the view for the top card.
            ViewData["CurrentTotalFunds"] = await _context.CompanyLedger.SumAsync(t => t.Amount);

            return View(approvalViewModels);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveBudget(int id)
        {
            var payroll = await _context.Payrolls.Include(p => p.PaySlips).FirstOrDefaultAsync(p => p.Id == id);
            if (payroll == null || payroll.Status != PayrollStatus.PendingApproval)
            {
                TempData["ErrorMessage"] = "Payroll not found or already actioned.";
                return RedirectToAction("Index");
            }

            // Perform the same historical check for security.
            var endOfMonth = payroll.PayrollMonth.AddMonths(1).AddDays(-1);
            var availableFunds = await _context.CompanyLedger.Where(t => t.TransactionDate <= endOfMonth).SumAsync(t => t.Amount);
            var totalPayrollCost = payroll.PaySlips.Sum(p => p.NetPay);

            if (availableFunds < totalPayrollCost)
            {
                TempData["ErrorMessage"] = "Cannot approve budget: Insufficient funds for that period.";
                return RedirectToAction("Index");
            }

            payroll.Status = PayrollStatus.BudgetApproved;
            payroll.DateApproved = DateTime.Now;
            payroll.ApprovedById = _userManager.GetUserId(User);

            // Add an expense transaction to the ledger
            _context.CompanyLedger.Add(new CompanyLedger
            {
                TransactionDate = DateTime.Now,
                Description = $"Payroll for {payroll.PayrollMonth:MMMM yyyy}",
                Amount = -totalPayrollCost // The amount is negative for an expense
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Budget for {payroll.PayrollMonth:MMMM yyyy} payroll has been approved.";
            return RedirectToAction("Index");
        }
    }
}


