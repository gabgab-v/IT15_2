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
    public class PayablesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly FinancialAnalyticsService _analyticsService;
        private readonly IAuditService _auditService;
        private readonly UserManager<IdentityUser> _userManager;

        public PayablesController(
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

            var summary = await _analyticsService.GetPayablesSummaryAsync(DateTime.UtcNow);
            var payables = await _context.AccountsPayables
                .Include(p => p.LedgerEntries)
                .AsNoTracking()
                .OrderByDescending(p => p.BillDate)
                .ToListAsync();

            var items = payables.Select(p =>
            {
                var paid = p.LedgerEntries
                    .Where(e => e.EntryType == LedgerEntryType.Expense)
                    .Sum(e => -e.Amount);

                var outstanding = p.BillAmount - paid;

                return new PayableListItemViewModel
                {
                    Id = p.Id,
                    SupplierName = p.SupplierName,
                    ReferenceNumber = p.ReferenceNumber,
                    BillDate = p.BillDate,
                    DueDate = p.DueDate,
                    BillAmount = p.BillAmount,
                    AmountPaid = paid,
                    OutstandingAmount = outstanding,
                    Status = p.Status,
                    ExpenseCategory = p.ExpenseCategory
                };
            }).ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                items = items
                    .Where(i =>
                        (!string.IsNullOrEmpty(i.SupplierName) && i.SupplierName.ToLowerInvariant().Contains(term)) ||
                        (!string.IsNullOrEmpty(i.ReferenceNumber) && i.ReferenceNumber.ToLowerInvariant().Contains(term)) ||
                        i.Status.ToString().ToLowerInvariant().Contains(term))
                    .ToList();
            }

            var model = new PayablesIndexViewModel
            {
                Summary = summary,
                Items = items
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var model = new PayableFormViewModel
            {
                CategoryOptions = GetExpenseCategoryOptions()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PayableFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.CategoryOptions = GetExpenseCategoryOptions();
                return View(model);
            }

            var payable = new AccountsPayable
            {
                SupplierName = model.SupplierName,
                ReferenceNumber = model.ReferenceNumber,
                BillDate = model.BillDate,
                DueDate = model.DueDate,
                BillAmount = model.BillAmount,
                ExpenseCategory = model.ExpenseCategory,
                Status = PayableStatus.Pending,
                Notes = model.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.AccountsPayables.Add(payable);
            await _context.SaveChangesAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(currentUser.Id, currentUser.UserName, "Payable Recorded",
                $"Accounts payable {payable.ReferenceNumber} for {payable.SupplierName} amounting to {payable.BillAmount:C} was recorded.");

            TempData["SuccessMessage"] = "Accounts payable recorded successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> RecordPayment(int id)
        {
            var payable = await _context.AccountsPayables
                .Include(p => p.LedgerEntries)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payable == null)
            {
                return NotFound();
            }

            if (payable.Status == PayableStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "This payable is cancelled and cannot be paid.";
                return RedirectToAction(nameof(Index));
            }

            var paid = payable.LedgerEntries
                .Where(e => e.EntryType == LedgerEntryType.Expense)
                .Sum(e => -e.Amount);

            var outstanding = Math.Max(0, payable.BillAmount - paid);

            var model = new PayablePaymentViewModel
            {
                PayableId = payable.Id,
                SupplierName = payable.SupplierName,
                ReferenceNumber = payable.ReferenceNumber,
                OutstandingAmount = outstanding,
                Description = $"Payment for bill {payable.ReferenceNumber}"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordPayment(PayablePaymentViewModel model)
        {
            var payable = await _context.AccountsPayables
                .Include(p => p.LedgerEntries)
                .FirstOrDefaultAsync(p => p.Id == model.PayableId);

            if (payable == null)
            {
                return NotFound();
            }

            if (payable.Status == PayableStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "This payable is cancelled and cannot be paid.";
                return RedirectToAction(nameof(Index));
            }

            var paid = payable.LedgerEntries
                .Where(e => e.EntryType == LedgerEntryType.Expense)
                .Sum(e => -e.Amount);

            var outstanding = Math.Max(0, payable.BillAmount - paid);

            if (outstanding <= 0)
            {
                ModelState.AddModelError(string.Empty, "This bill is already fully paid.");
            }

            if (model.Amount <= 0)
            {
                ModelState.AddModelError(nameof(model.Amount), "Payment amount must be greater than zero.");
            }

            if (model.Amount > outstanding)
            {
                ModelState.AddModelError(nameof(model.Amount), "Payment amount cannot exceed outstanding balance.");
            }

            if (!ModelState.IsValid)
            {
                model.SupplierName = payable.SupplierName;
                model.ReferenceNumber = payable.ReferenceNumber;
                model.OutstandingAmount = outstanding;
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);

            var ledgerEntry = new CompanyLedger
            {
                UserId = currentUser.Id,
                TransactionDate = model.PaymentDate,
                Description = string.IsNullOrWhiteSpace(model.Description)
                    ? $"Payment for bill {payable.ReferenceNumber}"
                    : model.Description,
                EntryType = LedgerEntryType.Expense,
                Category = payable.ExpenseCategory,
                ReferenceNumber = $"AP-{payable.Id:D6}-{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpperInvariant()}",
                Counterparty = payable.SupplierName,
                Amount = -model.Amount,
                AccountsPayableId = payable.Id
            };

            _context.CompanyLedger.Add(ledgerEntry);

            var newOutstanding = outstanding - model.Amount;
            payable.Status = newOutstanding <= 0m ? PayableStatus.Paid : PayableStatus.PartiallyPaid;
            payable.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(currentUser.Id, currentUser.UserName, "Payable Payment",
                $"Paid {model.Amount:C} for bill {payable.ReferenceNumber}. Remaining balance: {newOutstanding:C}.");

            TempData["SuccessMessage"] = "Payment recorded successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var payable = await _context.AccountsPayables.FindAsync(id);
            if (payable == null)
            {
                return NotFound();
            }

            payable.Status = PayableStatus.Cancelled;
            payable.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(currentUser.Id, currentUser.UserName, "Payable Cancelled",
                $"Bill {payable.ReferenceNumber} for {payable.SupplierName} was cancelled.");

            TempData["SuccessMessage"] = "Payable cancelled.";
            return RedirectToAction(nameof(Index));
        }

        private static IEnumerable<SelectListItem> GetExpenseCategoryOptions()
        {
            var allowedCategories = new[]
            {
                LedgerEntryCategory.Supplies,
                LedgerEntryCategory.Operations,
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
