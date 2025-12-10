using IT15.Data;
using IT15.Models;
using IT15.Services;
using IT15.ViewModels.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IT15.Areas.Accounting.Controllers
{
    [Area("Accounting")]
    [Authorize(Roles = "Accounting,Admin")]
    public class ReceivablesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly FinancialAnalyticsService _analyticsService;
        private readonly IAuditService _auditService;
        private readonly UserManager<IdentityUser> _userManager;

        public ReceivablesController(
            ApplicationDbContext context,
            FinancialAnalyticsService analyticsService,
            IAuditService auditService,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _analyticsService = analyticsService;
            _auditService = auditService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string search)
        {
            ViewData["Search"] = search;

            var summary = await _analyticsService.GetReceivablesSummaryAsync(DateTime.UtcNow);
            var receivables = await _context.AccountsReceivables
                .Include(r => r.LedgerEntries)
                .AsNoTracking()
                .OrderByDescending(r => r.InvoiceDate)
                .ToListAsync();

            var items = receivables.Select(r =>
            {
                var collected = r.LedgerEntries
                    .Where(e => e.EntryType == LedgerEntryType.Income)
                    .Sum(e => e.Amount);

                var outstanding = r.InvoiceAmount - collected;
                return new ReceivableListItemViewModel
                {
                    Id = r.Id,
                    CustomerName = r.CustomerName,
                    ReferenceNumber = r.ReferenceNumber,
                    InvoiceDate = r.InvoiceDate,
                    DueDate = r.DueDate,
                    InvoiceAmount = r.InvoiceAmount,
                    CollectedAmount = collected,
                    OutstandingAmount = outstanding,
                    Status = r.Status,
                    Category = r.RevenueCategory
                };
            }).ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                items = items
                    .Where(i =>
                        (!string.IsNullOrEmpty(i.CustomerName) && i.CustomerName.ToLowerInvariant().Contains(term)) ||
                        (!string.IsNullOrEmpty(i.ReferenceNumber) && i.ReferenceNumber.ToLowerInvariant().Contains(term)) ||
                        i.Status.ToString().ToLowerInvariant().Contains(term))
                    .ToList();
            }

            var model = new ReceivablesIndexViewModel
            {
                Summary = summary,
                Items = items
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var model = new ReceivableFormViewModel
            {
                CategoryOptions = GetRevenueCategoryOptions()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReceivableFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.CategoryOptions = GetRevenueCategoryOptions();
                return View(model);
            }

            var receivable = new AccountsReceivable
            {
                CustomerName = model.CustomerName,
                ReferenceNumber = model.ReferenceNumber,
                InvoiceDate = model.InvoiceDate,
                DueDate = model.DueDate,
                InvoiceAmount = model.InvoiceAmount,
                RevenueCategory = model.RevenueCategory,
                Status = ReceivableStatus.Pending,
                Notes = model.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.AccountsReceivables.Add(receivable);
            await _context.SaveChangesAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(currentUser.Id, currentUser.UserName, "Receivable Created",
                $"Receivable {receivable.ReferenceNumber} for {receivable.CustomerName} amounting to {receivable.InvoiceAmount:C} was created.");

            TempData["SuccessMessage"] = "Accounts receivable recorded successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> RecordPayment(int id)
        {
            var receivable = await _context.AccountsReceivables
                .Include(r => r.LedgerEntries)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receivable == null)
            {
                return NotFound();
            }

            if (receivable.Status == ReceivableStatus.WrittenOff)
            {
                TempData["ErrorMessage"] = "This receivable is written off and cannot be collected.";
                return RedirectToAction(nameof(Index));
            }

            var collected = receivable.LedgerEntries
                .Where(e => e.EntryType == LedgerEntryType.Income)
                .Sum(e => e.Amount);

            var outstanding = Math.Max(0, receivable.InvoiceAmount - collected);

            var model = new ReceivablePaymentViewModel
            {
                ReceivableId = receivable.Id,
                CustomerName = receivable.CustomerName,
                ReferenceNumber = receivable.ReferenceNumber,
                OutstandingAmount = outstanding,
                Description = $"Collection for invoice {receivable.ReferenceNumber}"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordPayment(ReceivablePaymentViewModel model)
        {
            var receivable = await _context.AccountsReceivables
                .Include(r => r.LedgerEntries)
                .FirstOrDefaultAsync(r => r.Id == model.ReceivableId);

            if (receivable == null)
            {
                return NotFound();
            }

            if (receivable.Status == ReceivableStatus.WrittenOff)
            {
                TempData["ErrorMessage"] = "This receivable is written off and cannot be collected.";
                return RedirectToAction(nameof(Index));
            }

            var collected = receivable.LedgerEntries
                .Where(e => e.EntryType == LedgerEntryType.Income)
                .Sum(e => e.Amount);

            var outstanding = Math.Max(0, receivable.InvoiceAmount - collected);

            if (outstanding <= 0)
            {
                ModelState.AddModelError(string.Empty, "This invoice is already fully collected.");
            }

            if (model.Amount <= 0)
            {
                ModelState.AddModelError(nameof(model.Amount), "Collection amount must be greater than zero.");
            }

            if (model.Amount > outstanding)
            {
                ModelState.AddModelError(nameof(model.Amount), "Collection amount cannot exceed outstanding balance.");
            }

            if (!ModelState.IsValid)
            {
                model.CustomerName = receivable.CustomerName;
                model.ReferenceNumber = receivable.ReferenceNumber;
                model.OutstandingAmount = outstanding;
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);

            var ledgerEntry = new CompanyLedger
            {
                UserId = currentUser.Id,
                TransactionDate = model.CollectionDate,
                Description = string.IsNullOrWhiteSpace(model.Description)
                    ? $"Collection for invoice {receivable.ReferenceNumber}"
                    : model.Description,
                EntryType = LedgerEntryType.Income,
                Category = receivable.RevenueCategory,
                ReferenceNumber = $"AR-{receivable.Id:D6}-{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpperInvariant()}",
                Counterparty = receivable.CustomerName,
                Amount = model.Amount,
                AccountsReceivableId = receivable.Id
            };

            _context.CompanyLedger.Add(ledgerEntry);

            var newOutstanding = outstanding - model.Amount;
            receivable.Status = newOutstanding <= 0m ? ReceivableStatus.Collected : ReceivableStatus.PartiallyCollected;
            receivable.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(currentUser.Id, currentUser.UserName, "Receivable Collection",
                $"Collected {model.Amount:C} for invoice {receivable.ReferenceNumber}. Remaining balance: {newOutstanding:C}.");

            TempData["SuccessMessage"] = "Collection recorded successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WriteOff(int id)
        {
            var receivable = await _context.AccountsReceivables.FindAsync(id);
            if (receivable == null)
            {
                return NotFound();
            }

            receivable.Status = ReceivableStatus.WrittenOff;
            receivable.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(currentUser.Id, currentUser.UserName, "Receivable Written Off",
                $"Invoice {receivable.ReferenceNumber} for {receivable.CustomerName} was written off.");

            TempData["SuccessMessage"] = "Receivable written off.";
            return RedirectToAction(nameof(Index));
        }

        private static IEnumerable<SelectListItem> GetRevenueCategoryOptions()
        {
            var allowedCategories = new[]
            {
                LedgerEntryCategory.Sales,
                LedgerEntryCategory.Other
            };

            return allowedCategories.Select(c => new SelectListItem
            {
                Value = c.ToString(),
                Text = c.ToString()
            });
        }
    }
}
