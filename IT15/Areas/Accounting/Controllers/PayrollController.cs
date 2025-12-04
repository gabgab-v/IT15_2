using IT15.Data;
using IT15.Models;
using IT15.Services;
using IT15.ViewModels.HumanResource;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IT15.Areas.Accounting.Controllers
{
    [Area("Accounting")]
    [Authorize(Roles = "Admin,Accounting")]
    public class PayrollController : Controller
    {
        private readonly PayrollService _payrollService;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IAuditService _auditService;
        private readonly UserManager<IdentityUser> _userManager;

        public PayrollController(
            PayrollService payrollService,
            ApplicationDbContext context,
            IEmailSender emailSender,
            IAuditService auditService,
            UserManager<IdentityUser> userManager)
        {
            _payrollService = payrollService;
            _context = context;
            _emailSender = emailSender;
            _auditService = auditService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var payrolls = await _context.Payrolls
                .Include(p => p.PaySlips)
                .ThenInclude(ps => ps.Employee)
                .OrderByDescending(p => p.PayrollMonth)
                .ToListAsync();
            return View(payrolls);
        }

        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            var payroll = await _context.Payrolls
                .Include(p => p.PaySlips)
                    .ThenInclude(ps => ps.Employee)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payroll == null)
            {
                return NotFound();
            }

            var viewModel = new PayrollReviewViewModel
            {
                Payroll = payroll,
                PaySlips = payroll.PaySlips.ToList(),
                TotalNetPayWithTaxes = payroll.PaySlips.Sum(ps => ps.NetPay + ps.TaxDeduction)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(DateTime payrollMonth)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var wasGenerated = await _payrollService.GeneratePayrollForMonth(payrollMonth);

            if (wasGenerated)
            {
                await _auditService.LogAsync(currentUser.Id, currentUser.UserName, "Payroll Generated", $"User '{currentUser.UserName}' generated a new payroll for {payrollMonth:MMMM yyyy}.");
                TempData["SuccessMessage"] = $"Payroll for {payrollMonth:MMMM yyyy} has been generated and sent for approval.";
            }
            else
            {
                await _auditService.LogAsync(currentUser.Id, currentUser.UserName, "Payroll Generation Failed", $"User '{currentUser.UserName}' failed to generate payroll for {payrollMonth:MMMM yyyy} (active payroll already exists).");
                TempData["ErrorMessage"] = $"Could not generate payroll. An active payroll for {payrollMonth:MMMM yyyy} already exists.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id)
        {
            var payroll = await _context.Payrolls.FindAsync(id);
            var currentUser = await _userManager.GetUserAsync(User);

            if (payroll != null && payroll.Status == PayrollStatus.PendingApproval)
            {
                payroll.IsArchived = true;
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(currentUser.Id, currentUser.UserName, "Payroll Archived", $"User '{currentUser.UserName}' archived the payroll for {payroll.PayrollMonth:MMMM yyyy}.");
                TempData["SuccessMessage"] = $"Payroll for {payroll.PayrollMonth:MMMM yyyy} has been archived.";
            }
            else
            {
                await _auditService.LogAsync(currentUser.Id, currentUser.UserName, "Payroll Archive Failed", $"User '{currentUser.UserName}' failed to archive payroll ID #{id}.");
                TempData["ErrorMessage"] = "Could not archive this payroll. It may have already been actioned.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Release(int id)
        {
            var payroll = await _context.Payrolls.FindAsync(id);
            var currentUser = await _userManager.GetUserAsync(User);

            if (payroll != null && payroll.Status == PayrollStatus.BudgetApproved)
            {
                payroll.Status = PayrollStatus.Completed;
                _context.Update(payroll);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(currentUser.Id, currentUser.UserName, "Payslips Released", $"User '{currentUser.UserName}' released payslips for the {payroll.PayrollMonth:MMMM yyyy} payroll.");
                TempData["SuccessMessage"] = $"Payslips for {payroll.PayrollMonth:MMMM yyyy} have been released.";
            }
            else
            {
                TempData["ErrorMessage"] = "Could not release payslips. Payroll has not been approved.";
            }
            return RedirectToAction("Index");
        }
    }
}

