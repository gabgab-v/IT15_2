using IT15.Data;
using IT15.Models;
using IT15.Services; // Add this to use the Audit Service
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IT15.Areas.Accounting.Controllers
{
    [Area("Accounting")]
    [Authorize(Roles = "Accounting,Admin")]
    public class OperationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService; // Inject the audit service

        public OperationsController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService; // Initialize the service
        }

        // GET: /Accounting/Operations
        public async Task<IActionResult> Index(string search)
        {
            ViewData["Search"] = search;

            // Fetch only operational costs (negative, not payroll or sales)
            var operationalCosts = await _context.CompanyLedger
                .Include(c => c.User)
                .Where(c => c.EntryType == LedgerEntryType.Expense && c.Category == LedgerEntryCategory.Operations)
                .OrderByDescending(c => c.TransactionDate)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                operationalCosts = operationalCosts
                    .Where(c =>
                        (!string.IsNullOrEmpty(c.Description) && c.Description.ToLowerInvariant().Contains(term)) ||
                        (!string.IsNullOrEmpty(c.Counterparty) && c.Counterparty.ToLowerInvariant().Contains(term)) ||
                        c.TransactionDate.ToString("MMM yyyy").ToLowerInvariant().Contains(term))
                    .ToList();
            }

            return View(operationalCosts);
        }

        // POST: /Accounting/Operations/AddCost
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCost(OperationalCostType costType, decimal amount, DateTime transactionMonth)
        {
            if (amount > 0)
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentUserName = User.Identity.Name;

                var costTransaction = new CompanyLedger
                {
                    UserId = currentUserId,
                    TransactionDate = new DateTime(transactionMonth.Year, transactionMonth.Month, 1),
                    Description = $"Operational Cost: {costType}",
                    EntryType = LedgerEntryType.Expense,
                    Category = costType == OperationalCostType.OfficeSupplies ? LedgerEntryCategory.Supplies : LedgerEntryCategory.Operations,
                    ReferenceNumber = $"OP-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant()}",
                    Counterparty = currentUserName ?? "Internal",
                    Amount = -amount // Costs are recorded as a negative amount
                };
                _context.CompanyLedger.Add(costTransaction);
                await _context.SaveChangesAsync();

                // --- AUDIT LOG ---
                await _auditService.LogAsync(currentUserId, currentUserName, "Operational Cost Added", $"User '{currentUserName}' recorded an expense of {amount:C} for {costType}.");

                TempData["SuccessMessage"] = $"Cost of {amount:C} for {costType} recorded successfully.";
            }
            return RedirectToAction("Index");
        }
    }
}

