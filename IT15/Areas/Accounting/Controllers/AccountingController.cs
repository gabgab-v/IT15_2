using IT15.Data;
using IT15.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IT15.Areas.Accounting.Controllers
{
    [Area("Accounting")]
    [Authorize(Roles = "Accounting,Admin")]
    public class AccountingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AccountingController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Accounting/Accounting/Index
        public async Task<IActionResult> Index()
        {
            // Fetch all payrolls that are waiting for budget approval
            var pendingPayrolls = await _context.Payrolls
                .Include(p => p.PaySlips)
                .Where(p => p.Status == PayrollStatus.PendingApproval)
                .OrderByDescending(p => p.PayrollMonth)
                .ToListAsync();

            return View(pendingPayrolls);
        }

        // POST: /Accounting/Accounting/ApproveBudget/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveBudget(int id)
        {
            var payroll = await _context.Payrolls.FindAsync(id);
            if (payroll != null && payroll.Status == PayrollStatus.PendingApproval)
            {
                payroll.Status = PayrollStatus.BudgetApproved;
                payroll.DateApproved = DateTime.Now;
                payroll.ApprovedById = _userManager.GetUserId(User);

                _context.Update(payroll);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Budget for {payroll.PayrollMonth:MMMM yyyy} payroll has been approved.";
            }
            return RedirectToAction("Index");
        }
    }
}

