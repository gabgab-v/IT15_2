using IT15.Data;
using IT15.Models;
using IT15.Services;
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
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IncomeApiService _incomeApiService;

        public SalesController(ApplicationDbContext context, IncomeApiService incomeApiService)
        {
            _context = context;
            _incomeApiService = incomeApiService;
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
                var saleTransaction = new CompanyLedger
                {
                    // Use the first day of the selected month for the transaction date
                    TransactionDate = new DateTime(transactionMonth.Year, transactionMonth.Month, 1),
                    Description = $"Sale of {productName}",
                    Amount = price
                };
                _context.CompanyLedger.Add(saleTransaction);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Sale for {transactionMonth:MMMM yyyy} recorded successfully!";
            }
            return RedirectToAction("Index");
        }
    }
}


