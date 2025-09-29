using IT15.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IT15.Models;
using IT15.Services;
using IT15.ViewModels.HumanResource;


namespace IT15.Areas.HumanResource.Controllers
{
    [Area("HumanResource")]
    [Authorize(Roles = "Admin,HumanResource")]
    public class LeaveRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ISmsSender _smsSender;
        private readonly IAuditService _auditService;

        public LeaveRequestController(ApplicationDbContext context, UserManager<IdentityUser> userManager, ISmsSender smsSender, IAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _smsSender = smsSender;
            _auditService = auditService;
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

            // THE CHANGE: Create a list of ViewModels that includes the leave balance for each request.
            var userIds = requests.Select(r => r.RequestingEmployeeId).Distinct().ToList();
            var userProfiles = await _context.UserProfiles
                .Where(p => userIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId, p => p.LeaveBalance);

            var viewModel = requests.Select(req => new LeaveRequestViewModel
            {
                LeaveRequest = req,
                AvailableLeaveDays = userProfiles.TryGetValue(req.RequestingEmployeeId, out var balance) ? balance : 0
            }).ToList();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var request = await _context.LeaveRequests
                .Include(r => r.RequestingEmployee)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request != null && request.Status == LeaveRequestStatus.Pending)
            {
                var approver = await _userManager.GetUserAsync(User);
                var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == request.RequestingEmployeeId);
                var requestedDays = (request.EndDate - request.StartDate).Days + 1;

                // THE FIX: Check the employee's leave balance before approving.
                if (userProfile != null && userProfile.LeaveBalance >= requestedDays)
                {
                    // Deduct the leave days from the user's balance.
                    userProfile.LeaveBalance -= requestedDays;

                    request.Status = LeaveRequestStatus.Approved;
                    request.DateActioned = DateTime.Now;
                    request.ApprovedById = approver.Id;

                    // Save changes to both the request and the user's profile.
                    await _context.SaveChangesAsync();

                    await _auditService.LogAsync(approver.Id, approver.UserName, "Leave Request Approved", $"HR user '{approver.UserName}' approved leave request #{request.Id} for '{request.RequestingEmployee.Email}'.");

                    if (!string.IsNullOrEmpty(request.RequestingEmployee.PhoneNumber))
                    {
                        var message = $"Your leave request from {request.StartDate:MMM dd} to {request.EndDate:MMM dd, yyyy} has been approved.";
                        await _smsSender.SendSmsAsync(request.RequestingEmployee.PhoneNumber, message);
                    }
                    TempData["SuccessMessage"] = "Leave request approved and balance updated.";
                }
                else
                {
                    // If the balance is insufficient, provide an error message.
                    await _auditService.LogAsync(approver.Id, approver.UserName, "Leave Approval Failed", $"HR user '{approver.UserName}' failed to approve leave for '{request.RequestingEmployee.Email}' (Insufficient Balance).");
                    TempData["ErrorMessage"] = $"Could not approve request. Employee only has {userProfile?.LeaveBalance ?? 0} days available.";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deny(int id)
        {
            var request = await _context.LeaveRequests
                .Include(r => r.RequestingEmployee)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request != null && request.Status == LeaveRequestStatus.Pending)
            {
                var denier = await _userManager.GetUserAsync(User);
                request.Status = LeaveRequestStatus.Denied;
                request.DateActioned = DateTime.Now;
                request.ApprovedById = denier.Id;
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(denier.Id, denier.UserName, "Leave Request Denied", $"HR user '{denier.UserName}' denied leave request #{request.Id} for '{request.RequestingEmployee.Email}'.");
                TempData["SuccessMessage"] = "Leave request has been denied.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

