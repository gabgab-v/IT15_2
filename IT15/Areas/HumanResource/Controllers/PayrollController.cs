using IT15.Data;
using IT15.Models;
using IT15.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IT15.Areas.HumanResource.Controllers
{
    [Area("HumanResource")]
    [Authorize(Roles = "Admin,HumanResource")]
    public class PayrollController : Controller
    {
        private readonly PayrollService _payrollService;
        private readonly ApplicationDbContext _context;

        public PayrollController(PayrollService payrollService, ApplicationDbContext context)
        {
            _payrollService = payrollService;
            _context = context;
        }

        // Updated Index action to fetch and display a list of all payrolls
        public async Task<IActionResult> Index()
        {
            var payrolls = await _context.Payrolls
                .Include(p => p.PaySlips) // Include payslips to calculate totals
                .OrderByDescending(p => p.PayrollMonth)
                .ToListAsync();
            return View(payrolls);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(DateTime payrollMonth)
        {
            // Check if payroll for this month already exists
            var existingPayroll = await _context.Payrolls.AnyAsync(p => p.PayrollMonth.Month == payrollMonth.Month && p.PayrollMonth.Year == payrollMonth.Year);
            if (existingPayroll)
            {
                TempData["ErrorMessage"] = $"Payroll for {payrollMonth:MMMM yyyy} has already been generated.";
            }
            else
            {
                await _payrollService.GeneratePayrollForMonth(payrollMonth);
                TempData["SuccessMessage"] = $"Payroll for {payrollMonth:MMMM yyyy} has been generated and sent for approval.";
            }
            return RedirectToAction("Index");
        }

        // NEW ACTION: Allows HR to finalize the payroll and make payslips visible
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Release(int id)
        {
            var payroll = await _context.Payrolls.FindAsync(id);
            if (payroll != null && payroll.Status == PayrollStatus.BudgetApproved)
            {
                payroll.Status = PayrollStatus.Completed;
                _context.Update(payroll);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Payslips for {payroll.PayrollMonth:MMMM yyyy} have been released.";
            }
            return RedirectToAction("Index");
        }
    }
}

