using IT15.Data;
using IT15.Models;
using IT15.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace IT15.Areas.HumanResource.Controllers
{
    [Area("HumanResource")]
    [Authorize(Roles = "Admin,HumanResource")]
    public class ApplicationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IAuditService _auditService;

        public ApplicationController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IEmailSender emailSender, IAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index()
        {
            var pendingApplications = await _context.JobApplications
                .Where(a => a.Status == ApplicationStatus.Pending)
                .OrderBy(a => a.DateApplied)
                .ToListAsync();
            return View(pendingApplications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var application = await _context.JobApplications.FindAsync(id);
            if (application == null || application.Status != ApplicationStatus.Pending)
            {
                return RedirectToAction("Index");
            }

            // Check if a user with this email or username already exists
            if (await _userManager.FindByEmailAsync(application.Email) != null || await _userManager.FindByNameAsync(application.Username) != null)
            {
                TempData["ErrorMessage"] = "An account with this email or username already exists.";
                return RedirectToAction("Index");
            }

            // THE FIX: Use a more robust check that won't crash on duplicates.
            var emailExists = await _userManager.Users.AnyAsync(u => u.NormalizedEmail == _userManager.NormalizeEmail(application.Email));
            var usernameExists = await _userManager.Users.AnyAsync(u => u.NormalizedUserName == _userManager.NormalizeName(application.Username));

            if (emailExists || usernameExists)
            {
                TempData["ErrorMessage"] = "An account with this email or username already exists. Please resolve the conflict before approving.";
                return RedirectToAction("Index");
            }

            application.Status = ApplicationStatus.Approved;
            application.EmailConfirmed = false;
            application.EmailConfirmationToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
            await _context.SaveChangesAsync();

            var confirmationUrl = Url.Action(
                "ConfirmEmail",
                "Apply",
                new { area = "", applicationId = application.Id, token = application.EmailConfirmationToken },
                protocol: Request.Scheme);
            var registerUrl = Url.Page(
                "/Account/Register",
                pageHandler: null,
                values: new { area = "Identity" },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(application.Email, "Your Application is Approved! Confirm Your Email",
                $"Hi {application.Username},<br/><br/>" +
                $"Your application has been approved. Please confirm your email to finish setting up your account by <a href='{HtmlEncoder.Default.Encode(confirmationUrl)}'>clicking here</a>.<br/><br/>" +
                $"After confirming, register with the same email and username you applied with, and choose your own password here: <a href='{HtmlEncoder.Default.Encode(registerUrl)}'>Complete Registration</a>.");

            var currentUser = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(currentUser.Id, currentUser.UserName, "Job Application Approved", $"Approved application for '{application.Username}'. Email confirmation sent to applicant.");
            TempData["SuccessMessage"] = "Application approved. A confirmation email has been sent to the applicant to verify their email and finish registration.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deny(int id)
        {
            var application = await _context.JobApplications.FindAsync(id);
            if (application != null && application.Status == ApplicationStatus.Pending)
            {
                application.Status = ApplicationStatus.Denied;
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditService.LogAsync(currentUser.Id, currentUser.UserName, "Job Application Denied", $"Denied application for '{application.Username}'.");

                TempData["SuccessMessage"] = $"Application for {application.Username} has been denied.";
            }
            else
            {
                TempData["ErrorMessage"] = "Could not find or action this application. It may have already been processed.";
            }
            return RedirectToAction("Index");
        }
    }
}

