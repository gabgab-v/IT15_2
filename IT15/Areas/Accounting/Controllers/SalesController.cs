using IT15.Data;
using IT15.Models;
using IT15.Services;
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
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IncomeApiService _incomeApiService;
        private readonly IAuditService _auditService; // Inject the audit service
        private readonly UserManager<IdentityUser> _userManager; // Inject UserManager

        public SalesController(
            ApplicationDbContext context,
            IncomeApiService incomeApiService,
            IAuditService auditService,
            UserManager<IdentityUser> userManager) // Request the new services
        {
            _context = context;
            _incomeApiService = incomeApiService;
            _auditService = auditService; // Initialize the service
            _userManager = userManager; // Initialize UserManager
        }

        public async Task<IActionResult> Index()
        {
            ViewData["CurrentBalance"] = await _context.CompanyLedger.SumAsync(t => t.Amount);
            var products = await _incomeApiService.GetProductsAsync();
            return View(products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordSale(string productName, decimal price, DateTime transactionMonth)
        {
            if (price > 0)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var saleTransaction = new CompanyLedger
                {
                    UserId = currentUser.Id,
                    TransactionDate = new DateTime(transactionMonth.Year, transactionMonth.Month, 1),
                    Description = $"Sale of {productName}",
                    EntryType = LedgerEntryType.Income,
                    Category = LedgerEntryCategory.Sales,
                    ReferenceNumber = $"SL-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant()}",
                    Counterparty = currentUser.UserName ?? "Internal",
                    Amount = price
                };
                _context.CompanyLedger.Add(saleTransaction);
                await _context.SaveChangesAsync();

                // --- AUDIT LOG ---
                await _auditService.LogAsync(currentUser.Id, currentUser.UserName, "Sale Recorded", $"User '{currentUser.UserName}' recorded a sale of {productName} for {price:C}.");

                TempData["SuccessMessage"] = $"Sale for {transactionMonth:MMMM yyyy} recorded successfully!";
            }
            return RedirectToAction("Index");
        }
    }
}

