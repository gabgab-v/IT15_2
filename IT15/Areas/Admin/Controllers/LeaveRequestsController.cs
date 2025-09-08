using IT15.Data;
using IT15.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IT15.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class LeaveRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public LeaveRequestController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Admin/LeaveRequest
        public async Task<IActionResult> Index(string searchString, string statusFilter)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["StatusFilter"] = statusFilter;

            // THE FIX: Explicitly define the query type as IQueryable<LeaveRequest>
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

        // POST: /Admin/LeaveRequest/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest != null)
            {
                leaveRequest.Status = LeaveRequestStatus.Approved;
                leaveRequest.ApprovedById = User.FindFirstValue(ClaimTypes.NameIdentifier);
                leaveRequest.DateActioned = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/LeaveRequest/Deny/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deny(int id)
        {
            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest != null)
            {
                leaveRequest.Status = LeaveRequestStatus.Denied;
                leaveRequest.ApprovedById = User.FindFirstValue(ClaimTypes.NameIdentifier);
                leaveRequest.DateActioned = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

