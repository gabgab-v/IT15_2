using IT15.Data;
using IT15.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace IT15.Areas.HumanResource.Controllers
{
    [Area("HumanResource")]
    [Authorize(Roles = "Admin,HumanResource")]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AttendanceController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /HumanResource/Attendance/Index
        // This now shows a list of Overtime Requests, not Daily Logs.
        public async Task<IActionResult> Index()
        {
            var pendingRequests = await _context.OvertimeRequests
                .Include(r => r.RequestingEmployee)
                .Where(r => r.Status == OvertimeStatus.PendingApproval)
                .OrderByDescending(r => r.DateRequested)
                .ToListAsync();

            return View(pendingRequests);
        }

        // POST: /HumanResource/Attendance/ApproveRequest/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRequest(int id)
        {
            var request = await _context.OvertimeRequests.FindAsync(id);
            if (request != null && request.Status == OvertimeStatus.PendingApproval)
            {
                request.Status = OvertimeStatus.Approved;
                request.ApprovedById = _userManager.GetUserId(User);
                request.DateActioned = System.DateTime.Now;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        // POST: /HumanResource/Attendance/DenyRequest/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DenyRequest(int id)
        {
            var request = await _context.OvertimeRequests.FindAsync(id);
            if (request != null && request.Status == OvertimeStatus.PendingApproval)
            {
                request.Status = OvertimeStatus.Denied;
                request.ApprovedById = _userManager.GetUserId(User);
                request.DateActioned = System.DateTime.Now;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}

