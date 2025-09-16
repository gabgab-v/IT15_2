using IT15.Data;
using IT15.Models;
using IT15.Services;
using IT15.ViewModels.HumanResource;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IT15.Areas.HumanResource.Controllers
{
    [Area("HumanResource")]
    [Authorize(Roles = "Admin,HumanResource")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly HolidayApiService _holidayApiService;

        public DashboardController(ApplicationDbContext context, HolidayApiService holidayApiService)
        {
            _context = context;
            _holidayApiService = holidayApiService;
        }

        // THE CHANGE: The Index action now accepts startDate and endDate parameters.
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            var today = DateTime.Today;
            // Default the date range to the last 7 days if not provided.
            var start = startDate ?? today.AddDays(-6);
            var end = endDate ?? today;

            // --- Fetch Data for Stat Cards (remains the same) ---
            var pendingLeaveRequests = await _context.LeaveRequests.CountAsync(r => r.Status == LeaveRequestStatus.Pending);
            var onLeaveToday = await _context.LeaveRequests.CountAsync(r => r.Status == LeaveRequestStatus.Approved && r.StartDate <= today && r.EndDate >= today);
            var pendingPayrolls = await _context.Payrolls.CountAsync(p => p.Status == PayrollStatus.PendingApproval);

            // --- Fetch Holiday Data (remains the same) ---
            var upcomingHolidays = (await _holidayApiService.GetUpcomingHolidaysAsync("PH")).Take(5).ToList();

            // --- UPDATED CHART DATA LOGIC ---
            var attendanceData = await _context.DailyLogs
                // Use the new date range for the query
                .Where(d => d.CheckInTime.Date >= start.Date && d.CheckInTime.Date <= end.Date)
                .GroupBy(d => d.CheckInTime.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Date, x => x.Count);

            var chartLabels = new List<string>();
            var chartData = new List<int>();

            // Loop through the selected date range to build the chart
            for (var date = start; date <= end; date = date.AddDays(1))
            {
                chartLabels.Add(date.ToString("MMM dd"));
                chartData.Add(attendanceData.ContainsKey(date.Date) ? attendanceData[date.Date] : 0);
            }

            var viewModel = new HumanResourceDashboardViewModel
            {
                PendingLeaveRequestsCount = pendingLeaveRequests,
                EmployeesOnLeaveToday = onLeaveToday,
                PayrollsPendingApprovalCount = pendingPayrolls,
                UpcomingHolidays = upcomingHolidays,
                AttendanceChartLabels = chartLabels,
                AttendanceChartData = chartData,
                StartDate = start,
                EndDate = end
            };

            return View(viewModel);
        }
    }
}

