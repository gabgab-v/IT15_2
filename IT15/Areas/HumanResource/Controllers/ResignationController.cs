using IT15.Data;
using IT15.Models;
using IT15.Services;
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
    public class ResignationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAuditService _auditService;

        public ResignationController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index()
        {
            var requests = await _context.ResignationRequests
                .Include(r => r.RequestingEmployee)
                .OrderBy(r => r.Status)
                .ThenBy(r => r.EffectiveDate)
                .ToListAsync();
            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var request = await _context.ResignationRequests
                .Include(r => r.RequestingEmployee)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request != null && request.Status == ResignationStatus.Pending)
            {
                var approver = await _userManager.GetUserAsync(User);

                // 1. Approve the request
                request.Status = ResignationStatus.Approved;
                request.DateActioned = DateTime.UtcNow;
                request.ApprovedById = approver.Id;

                // 2. Archive the user by updating their profile
                var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == request.RequestingEmployeeId);
                if (userProfile != null)
                {
                    userProfile.IsArchived = true;
                }

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(approver.Id, approver.UserName, "Resignation Approved", $"HR user '{approver.UserName}' approved resignation for '{request.RequestingEmployee.UserName}'. User has been archived.");
                TempData["SuccessMessage"] = $"Resignation for {request.RequestingEmployee.Email} has been approved and the user has been archived.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deny(int id)
        {
            var request = await _context.ResignationRequests
               .Include(r => r.RequestingEmployee)
               .FirstOrDefaultAsync(r => r.Id == id);

            if (request != null && request.Status == ResignationStatus.Pending)
            {
                var denier = await _userManager.GetUserAsync(User);
                request.Status = ResignationStatus.Denied;
                request.DateActioned = DateTime.UtcNow;
                request.ApprovedById = denier.Id;

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(denier.Id, denier.UserName, "Resignation Denied", $"HR user '{denier.UserName}' denied resignation for '{request.RequestingEmployee.UserName}'.");
                TempData["SuccessMessage"] = $"Resignation for {request.RequestingEmployee.Email} has been denied.";
            }
            return RedirectToAction("Index");
        }
    }
}

