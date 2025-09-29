using IT15.Data;
using IT15.Models;
using IT15.Services; // Add this to use the Audit Service
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims; // Required for User.FindFirstValue
using System.Threading.Tasks;

namespace IT15.Areas.HumanResource.Controllers
{
    [Area("HumanResource")]
    [Authorize(Roles = "Admin,HumanResource")]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAuditService _auditService; // Inject the audit service

        public AttendanceController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService; // Initialize the service
        }

        // GET: /HumanResource/Attendance/Index
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
            // Include employee data to log their details
            var request = await _context.OvertimeRequests
                .Include(r => r.RequestingEmployee)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request != null && request.Status == OvertimeStatus.PendingApproval)
            {
                var approverId = _userManager.GetUserId(User);
                request.Status = OvertimeStatus.Approved;
                request.ApprovedById = approverId;
                request.DateActioned = System.DateTime.Now;
                await _context.SaveChangesAsync();

                // --- AUDIT LOG ---
                var approverName = User.Identity.Name;
                await _auditService.LogAsync(approverId, approverName, "Overtime Approved", $"HR user '{approverName}' approved overtime request #{request.Id} for '{request.RequestingEmployee.Email}'.");
            }
            return RedirectToAction("Index");
        }

        // POST: /HumanResource/Attendance/DenyRequest/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DenyRequest(int id)
        {
            var request = await _context.OvertimeRequests
                .Include(r => r.RequestingEmployee)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request != null && request.Status == OvertimeStatus.PendingApproval)
            {
                var denierId = _userManager.GetUserId(User);
                request.Status = OvertimeStatus.Denied;
                request.ApprovedById = denierId;
                request.DateActioned = System.DateTime.Now;
                await _context.SaveChangesAsync();

                // --- AUDIT LOG ---
                var denierName = User.Identity.Name;
                await _auditService.LogAsync(denierId, denierName, "Overtime Denied", $"HR user '{denierName}' denied overtime request #{request.Id} for '{request.RequestingEmployee.Email}'.");
            }
            return RedirectToAction("Index");
        }
    }
}

