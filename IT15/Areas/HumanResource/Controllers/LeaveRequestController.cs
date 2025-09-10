using IT15.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using IT15.Models;

namespace IT15.Areas.HumanResource.Controllers
{
    [Area("HumanResource")]
    // Allow both HR and Admin to access this feature
    [Authorize(Roles = "Admin,HumanResource")]
    public class LeaveRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public LeaveRequestController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string searchString, string statusFilter)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["StatusFilter"] = statusFilter;

            IQueryable<LeaveRequest> requestsQuery = _context.LeaveRequests.Include(r => r.RequestingEmployee);

            if (!String.IsNullOrEmpty(searchString))
            {
                requestsQuery = requestsQuery.Where(r => r.RequestingEmployee.Email.Contains(searchString));
            }

            if (!String.IsNullOrEmpty(statusFilter) && Enum.TryParse<LeaveRequestStatus>(statusFilter, out var status))
            {
                requestsQuery = requestsQuery.Where(r => r.Status == status);
            }

            var requests = await requestsQuery.OrderByDescending(r => r.DateRequested).ToListAsync();
            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var request = await _context.LeaveRequests.FindAsync(id);
            if (request != null)
            {
                request.Status = LeaveRequestStatus.Approved;
                request.DateActioned = DateTime.Now;
                request.ApprovedById = _userManager.GetUserId(User);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deny(int id)
        {
            var request = await _context.LeaveRequests.FindAsync(id);
            if (request != null)
            {
                request.Status = LeaveRequestStatus.Denied;
                request.DateActioned = DateTime.Now;
                request.ApprovedById = _userManager.GetUserId(User);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
