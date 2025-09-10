using IT15.Data;
using IT15.Models;
using IT15.ViewModels.HumanResource;
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
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            var viewModel = new HumanResourceDashboardViewModel
            {
                PendingLeaveRequestsCount = await _context.LeaveRequests
                    .CountAsync(r => r.Status == LeaveRequestStatus.Pending),

                EmployeesOnLeaveToday = await _context.LeaveRequests
                    .CountAsync(r => r.Status == LeaveRequestStatus.Approved && r.StartDate <= today && r.EndDate >= today),

                PayrollsPendingApprovalCount = await _context.Payrolls
                    .CountAsync(p => p.Status == PayrollStatus.PendingApproval)
            };

            return View(viewModel);
        }
    }
}
