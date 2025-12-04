using IT15.Data;
using IT15.Models;
using IT15.ViewModels.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using IT15.Services; // Add this to use the Audit Service

namespace IT15.Areas.Accounting.Controllers
{
    [Area("Accounting")]
    [Authorize(Roles = "Accounting,Admin")]
    public class AccountingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AccountingController> _logger;
        private readonly IAuditService _auditService; // Inject the audit service

        public AccountingController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<AccountingController> logger,
            IAuditService auditService) // Request the service
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _auditService = auditService; // Initialize the service
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

            var endOfMonth = payroll.PayrollMonth.AddMonths(1).AddDays(-1);
            var availableFunds = await _context.CompanyLedger.Where(t => t.TransactionDate <= endOfMonth).SumAsync(t => t.Amount);
            var totalPayrollCost = payroll.PaySlips.Sum(p => p.NetPay + p.TaxDeduction);

            var currentUser = await _userManager.GetUserAsync(User);

            if (availableFunds < totalPayrollCost)
            {
                // --- AUDIT LOG for failed attempt ---
                await _auditService.LogAsync(currentUser.Id, currentUser.UserName, "Payroll Approval Failed", $"User '{currentUser.UserName}' failed to approve payroll for {payroll.PayrollMonth:MMMM yyyy} due to insufficient funds.");
                TempData["ErrorMessage"] = "Cannot approve budget: Insufficient funds for that period.";
                return RedirectToAction("Index");
            }

            payroll.Status = PayrollStatus.BudgetApproved;
            payroll.DateApproved = DateTime.UtcNow;
            payroll.ApprovedById = currentUser.Id;

            _context.CompanyLedger.Add(new CompanyLedger
            {
                UserId = currentUser.Id,
                TransactionDate = DateTime.UtcNow,
                Description = $"Payroll expense for {payroll.PayrollMonth:MMMM yyyy}",
                EntryType = LedgerEntryType.Expense,
                Category = LedgerEntryCategory.Payroll,
                ReferenceNumber = $"PY-{payroll.PayrollMonth:yyyyMM}-{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpperInvariant()}",
                Counterparty = "Payroll",
                Amount = -totalPayrollCost
            });

            await _context.SaveChangesAsync();

            // --- AUDIT LOG for successful approval ---
            await _auditService.LogAsync(currentUser.Id, currentUser.UserName, "Payroll Budget Approved", $"User '{currentUser.UserName}' approved the budget for the {payroll.PayrollMonth:MMMM yyyy} payroll.");

            TempData["SuccessMessage"] = $"Budget for {payroll.PayrollMonth:MMMM yyyy} payroll has been approved.";
            return RedirectToAction("Index");
        }
    }
}

