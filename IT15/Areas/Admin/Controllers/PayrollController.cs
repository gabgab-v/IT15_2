using IT15.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace IT15.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PayrollController : Controller
    {
        private readonly PayrollService _payrollService;

        public PayrollController(PayrollService payrollService)
        {
            _payrollService = payrollService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(DateTime payrollMonth)
        {
            await _payrollService.GeneratePayrollForMonth(payrollMonth);
            TempData["SuccessMessage"] = $"Payroll for {payrollMonth:MMMM yyyy} generated successfully!";
            return RedirectToAction("Index");
        }
    }
}

