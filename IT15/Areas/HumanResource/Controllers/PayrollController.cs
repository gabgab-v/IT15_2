using IT15.Data;
using IT15.Models;
using IT15.Services;
using IT15.ViewModels.HumanResource;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
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
        private readonly IEmailSender _emailSender;

        public PayrollController(PayrollService payrollService, ApplicationDbContext context, IEmailSender emailSender)
        {
            _payrollService = payrollService;
            _context = context;
            _emailSender = emailSender;
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
                TotalNetPay = payroll.PaySlips.Sum(ps => ps.NetPay)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(DateTime payrollMonth)
        {
            var wasGenerated = await _payrollService.GeneratePayrollForMonth(payrollMonth);

            if (wasGenerated)
            {
                TempData["SuccessMessage"] = $"Payroll for {payrollMonth:MMMM yyyy} has been generated and sent for approval.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Could not generate payroll. An active payroll for {payrollMonth:MMMM yyyy} already exists.";
            }

            return RedirectToAction("Index");
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id)
        {
            var payroll = await _context.Payrolls.FindAsync(id);
            if (payroll != null && payroll.Status == PayrollStatus.PendingApproval)
            {
                payroll.IsArchived = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Payroll for {payroll.PayrollMonth:MMMM yyyy} has been archived.";
            }
            else
            {
                TempData["ErrorMessage"] = "Could not archive this payroll. It may have already been actioned by accounting.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPayslip(int id)
        {
            var payslip = await _context.PaySlips
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payslip == null || payslip.Employee == null || string.IsNullOrEmpty(payslip.Employee.Email))
            {
                TempData["ErrorMessage"] = "Could not send email. Payslip or employee email not found.";
                return RedirectToAction("Index");
            }

            var payroll = await _context.Payrolls.FindAsync(payslip.PayrollId);
            string subject = $"Your Payslip for {payroll.PayrollMonth:MMMM yyyy}";
            string message = $@"
                Hi {payslip.Employee.UserName},<br/><br/>
                Here is your Payslip summary:<br/>
                <ul>
                    <li><strong>Basic Salary:</strong> ₱{payslip.BasicSalary:N2}</li>
                    <li><strong>Deductions:</strong> ₱{payslip.TotalDeductions:N2}</li>
                    <li><strong>Net Pay:</strong> ₱{payslip.NetPay:N2}</li>
                </ul>
                <br/>Thank you,<br/>The HR Team";

            try
            {
                await _emailSender.SendEmailAsync(payslip.Employee.Email, subject, message);
                TempData["SuccessMessage"] = $"Payslip successfully emailed to {payslip.Employee.Email}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error sending email: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}

