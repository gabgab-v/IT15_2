using IT15.Data;
using IT15.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

        public async Task<bool> GeneratePayrollForMonth(DateTime month)
        {
            var payrollMonth = new DateTime(month.Year, month.Month, 1);
            bool activePayrollExists = await _context.Payrolls
                .AnyAsync(p => p.PayrollMonth == payrollMonth && !p.IsArchived);

            if (activePayrollExists)
            {
                return false;
            }

            var payroll = new Payroll
            {
                PayrollMonth = payrollMonth,
                DateGenerated = DateTime.Now,
                Status = PayrollStatus.PendingApproval
            };

            var employees = await _userManager.GetUsersInRoleAsync("User");

            foreach (var employee in employees)
            {
                decimal basicSalary = 20000;
                int workingDaysInMonth = DateTime.DaysInMonth(month.Year, month.Month);

                var attendanceCount = await _context.DailyLogs
                    .CountAsync(d => d.UserId == employee.Id && d.CheckInTime.Month == month.Month && d.CheckInTime.Year == month.Year);

                var approvedLeaves = await _context.LeaveRequests
                    .CountAsync(l => l.RequestingEmployeeId == employee.Id && l.Status == LeaveRequestStatus.Approved && l.StartDate.Month == month.Month);

                int daysPresent = attendanceCount + approvedLeaves;
                int daysAbsent = workingDaysInMonth > daysPresent ? workingDaysInMonth - daysPresent : 0;
                decimal dailyRate = workingDaysInMonth > 0 ? basicSalary / workingDaysInMonth : 0;
                decimal absentDeductions = daysAbsent * dailyRate;

                // --- NEW OVERTIME CALCULATION LOGIC ---
                // 1. Get all approved overtime requests for this employee for the month.
                var approvedOvertimeRequests = await _context.OvertimeRequests
                    .Where(r => r.RequestingEmployeeId == employee.Id &&
                                r.Status == OvertimeStatus.Approved &&
                                r.OvertimeDate.Month == month.Month &&
                                r.OvertimeDate.Year == month.Year)
                    .ToListAsync();

                // 2. Sum the total approved hours from those requests.
                decimal totalApprovedOvertimeHours = approvedOvertimeRequests.Sum(r => r.TotalHours);

                // 3. Calculate pay based on approved hours.
                decimal hourlyRate = dailyRate / 8; // Assuming an 8-hour day
                decimal overtimePay = totalApprovedOvertimeHours * hourlyRate * 1.25m; // Assuming 125% overtime rate
                // --- END OF NEW LOGIC ---

                decimal sss = 500;
                decimal philhealth = 300;
                decimal pagibig = 100;

                decimal grossPay = (basicSalary - absentDeductions) + overtimePay;
                grossPay = Math.Max(0, grossPay);

                decimal totalDeductions = sss + philhealth + pagibig + absentDeductions;
                decimal netPay = Math.Max(0, grossPay - totalDeductions);

                var paySlip = new PaySlip { /* ... mapping ... */ };
                payroll.PaySlips.Add(paySlip);
            }

            _context.Payrolls.Add(payroll);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

