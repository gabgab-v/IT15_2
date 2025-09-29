using IT15.Data;
using IT15.Models;
using IT15.Services;
using IT15.ViewModels.HumanResource;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IT15.Areas.HumanResource.Controllers
{
    [Area("HumanResource")]
    [Authorize(Roles = "Admin,HumanResource")] // Restored Admin access for consistency
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAuditService _auditService; // Added back the Audit Service

        public EmployeeController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService; // Initialize the service
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
            if (id == null) return NotFound();
            var employee = await _userManager.FindByIdAsync(id);
            if (employee == null) return NotFound();

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
                    var overtimeStart = request.OvertimeDate.Date.AddHours(18);
                    if (dailyLog.CheckOutTime.Value > overtimeStart)
                    {
                        var actualOvertimeHours = (decimal)(dailyLog.CheckOutTime.Value - overtimeStart).TotalHours;
                        totalOvertimePay += actualOvertimeHours * hourlyRate * 1.25m;
                    }
                }
            }

            decimal sss = 500, philhealth = 300, pagibig = 100;
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

        // --- Actions for Managing Leave Balances ---

        [HttpGet]
        public async Task<IActionResult> LeaveBalances()
        {
            var employees = await _userManager.GetUsersInRoleAsync("User");
            var userIds = employees.Select(e => e.Id).ToList();

            // This part is crucial for creating profiles for any users who might be missing one.
            foreach (var userId in userIds)
            {
                var profileExists = await _context.UserProfiles.AnyAsync(p => p.UserId == userId);
                if (!profileExists)
                {
                    _context.UserProfiles.Add(new UserProfile { UserId = userId, LeaveBalance = 3 });
                }
            }
            await _context.SaveChangesAsync(); // Save any new profiles

            var userProfiles = await _context.UserProfiles
                .Where(p => userIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId, p => p.LeaveBalance);

            var viewModel = employees.Select(emp => new EmployeeLeaveBalanceViewModel
            {
                Employee = emp,
                CurrentLeaveBalance = userProfiles.TryGetValue(emp.Id, out var balance) ? balance : 0
            }).ToList();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLeaveDays(string userId, int daysToAdd)
        {
            if (daysToAdd > 0)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("LeaveBalances");
                }

                var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
                if (userProfile == null)
                {
                    userProfile = new UserProfile { UserId = userId, LeaveBalance = 0 };
                    _context.UserProfiles.Add(userProfile);
                }

                userProfile.LeaveBalance += daysToAdd;
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditService.LogAsync(currentUser.Id, currentUser.UserName, "Leave Balance Updated", $"HR user '{currentUser.UserName}' added {daysToAdd} leave days to '{user.UserName}'. New balance: {userProfile.LeaveBalance}.");

                TempData["SuccessMessage"] = $"Successfully added {daysToAdd} leave days to {user.Email}.";
            }
            else
            {
                // THE CHANGE: Add a specific error message for invalid input.
                TempData["ErrorMessage"] = "Please enter a positive number of days to add.";
            }
            return RedirectToAction("LeaveBalances");
        }
    }
}

