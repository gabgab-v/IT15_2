using IT15.Data;
using IT15.Models;
using IT15.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IT15.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public DashboardController(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.UtcNow.Date;

            var viewModel = new DashboardViewModel
            {
                TotalUsers = await _userManager.Users.CountAsync(),

                AttendanceTodayCount = await _context.DailyLogs
                    .CountAsync(log => log.CheckInTime.Date == today),

                UsersOnLeaveToday = await _context.LeaveRequests
                    .CountAsync(req => req.Status == LeaveRequestStatus.Approved &&
                                       today >= req.StartDate.Date &&
                                       today <= req.EndDate.Date)
            };

            return View(viewModel);
        }
    }
}

