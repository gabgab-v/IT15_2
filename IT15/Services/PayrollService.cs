using Humanizer;
using IT15.Data;
using IT15.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IT15.Services
{
    public class PayrollService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PayrollService(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task GeneratePayrollForMonth(DateTime month)
        {
            var payroll = new Payroll
            {
                PayrollMonth = new DateTime(month.Year, month.Month, 1),
                DateGenerated = DateTime.Now
            };

            var employees = await _userManager.GetUsersInRoleAsync("User");

            foreach (var employee in employees)
            {
                // --- CALCULATION LOGIC ---
                // This is a simplified example. A real system would be more complex.
                decimal basicSalary = 20000; // Assume a fixed monthly salary for simplicity
                int workingDaysInMonth = DateTime.DaysInMonth(month.Year, month.Month);

                var attendance = await _context.DailyLogs
                    .Where(d => d.UserId == employee.Id && d.CheckInTime.Month == month.Month && d.CheckInTime.Year == month.Year)
                    .CountAsync();

                var approvedLeaves = await _context.LeaveRequests
                    .Where(l => l.RequestingEmployeeId == employee.Id && l.Status == LeaveRequestStatus.Approved && l.StartDate.Month == month.Month)
                    .CountAsync();

                int daysPresent = attendance + approvedLeaves;
                int daysAbsent = workingDaysInMonth - daysPresent;
                decimal dailyRate = basicSalary / workingDaysInMonth;
                decimal absentDeductions = daysAbsent > 0 ? daysAbsent * dailyRate : 0;

                // Simplified fixed deductions
                decimal sss = 500;
                decimal philhealth = 300;
                decimal pagibig = 100;

                decimal grossPay = basicSalary - absentDeductions;
                decimal totalDeductions = sss + philhealth + pagibig + absentDeductions;
                decimal netPay = grossPay - totalDeductions;

                var paySlip = new PaySlip
                {
                    EmployeeId = employee.Id,
                    BasicSalary = basicSalary,
                    AbsentDeductions = absentDeductions,
                    SSSDeduction = sss,
                    PhilHealthDeduction = philhealth,
                    PagIBIGDeduction = pagibig,
                    TaxDeduction = 0, // Simplified
                    GrossPay = grossPay,
                    TotalDeductions = totalDeductions,
                    NetPay = netPay
                };
                payroll.PaySlips.Add(paySlip);
            }

            _context.Payrolls.Add(payroll);
            await _context.SaveChangesAsync();
        }
    }
}
