using IT15.Data;
using IT15.Models;
using IT15.ViewModels.HumanResource;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IT15.Areas.HumanResource.Controllers
{
    [Area("HumanResource")]
    [Authorize(Roles = "Admin,HumanResource")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public EmployeeController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /HumanResource/Employee
        public async Task<IActionResult> Index()
        {
            var employees = await _userManager.GetUsersInRoleAsync("User");
            return View(employees);
        }

        // GET: /HumanResource/Employee/PayrollOverview/{id}
        public async Task<IActionResult> PayrollOverview(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _userManager.FindByIdAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            // --- Live Payroll Calculation Logic ---
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);
            var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);

            var attendanceLogs = await _context.DailyLogs
                .Where(d => d.UserId == id && d.CheckInTime >= startOfMonth && d.CheckInTime < endOfMonth)
                .ToListAsync();

            var approvedLeaves = await _context.LeaveRequests
                .CountAsync(l => l.RequestingEmployeeId == id && l.Status == LeaveRequestStatus.Approved && l.StartDate >= startOfMonth && l.StartDate < endOfMonth);

            int daysPresent = attendanceLogs.Count + approvedLeaves;
            int daysAbsent = daysInMonth > daysPresent ? daysInMonth - daysPresent : 0;

            decimal basicSalary = 20000;
            decimal dailyRate = daysInMonth > 0 ? basicSalary / daysInMonth : 0;
            decimal absentDeductions = daysAbsent * dailyRate;
            decimal hourlyRate = dailyRate / 8;

            // --- NEW OVERTIME AND PENALTY LOGIC ---
            decimal totalOvertimePay = 0;
            decimal totalOvertimePenalty = 0;
            decimal totalApprovedOvertimeHours = 0;

            var approvedRequests = await _context.OvertimeRequests
                .Where(r => r.RequestingEmployeeId == id && r.Status == OvertimeStatus.Approved && r.OvertimeDate >= startOfMonth && r.OvertimeDate < endOfMonth)
                .ToListAsync();

            totalApprovedOvertimeHours = approvedRequests.Sum(r => r.TotalHours);

            foreach (var request in approvedRequests)
            {
                var dailyLog = attendanceLogs.FirstOrDefault(l => l.CheckInTime.Date == request.OvertimeDate.Date);

                if (dailyLog != null && dailyLog.CheckOutTime.HasValue)
                {
                    var approvedEndTime = request.OvertimeDate.Date + request.EndTime;

                    if (dailyLog.CheckOutTime.Value < approvedEndTime)
                    {
                        var unworkedHours = (decimal)(approvedEndTime - dailyLog.CheckOutTime.Value).TotalHours;
                        totalOvertimePenalty += unworkedHours * hourlyRate;
                    }

                    var overtimeStart = request.OvertimeDate.Date.AddHours(18); // Assumes OT starts at 6 PM
                    if (dailyLog.CheckOutTime.Value > overtimeStart)
                    {
                        var actualOvertimeHours = (decimal)(dailyLog.CheckOutTime.Value - overtimeStart).TotalHours;
                        totalOvertimePay += actualOvertimeHours * hourlyRate * 1.25m;
                    }
                }
            }

            decimal sss = 500;
            decimal philhealth = 300;
            decimal pagibig = 100;

            decimal grossPay = Math.Max(0, (basicSalary - absentDeductions) + totalOvertimePay);
            decimal totalDeductions = sss + philhealth + pagibig + absentDeductions + totalOvertimePenalty;
            decimal netPay = Math.Max(0, grossPay - totalDeductions);

            var viewModel = new EmployeePayrollViewModel
            {
                Employee = employee,
                DaysPresent = daysPresent,
                DaysAbsent = daysAbsent,
                ApprovedOvertimeHours = totalApprovedOvertimeHours,
                BasicSalary = basicSalary,
                OvertimePay = totalOvertimePay,
                OvertimePenalty = totalOvertimePenalty,
                GrossPay = grossPay,
                AbsentDeductions = absentDeductions,
                SssDeduction = sss,
                PhilHealthDeduction = philhealth,
                PagIbigDeduction = pagibig,
                TotalDeductions = totalDeductions,
                NetPay = netPay
            };

            return View(viewModel);
        }
    }
}

