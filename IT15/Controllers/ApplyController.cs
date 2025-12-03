using IT15.Data;
using IT15.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using IT15.ViewModels;

namespace IT15.Controllers
{
    public class ApplyController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ApplyController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // Pass a new JobApplication model to the view to set default values
            return View(new JobApplication());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(JobApplication application)
        {
            if (ModelState.IsValid)
            {
                // THE FIX: Combine the country code and phone number into a single string.
                application.PhoneNumber = application.CountryCode + application.PhoneNumber;

                application.DateApplied = DateTime.UtcNow;
                application.Status = ApplicationStatus.Pending;

                _context.JobApplications.Add(application);
                await _context.SaveChangesAsync();

                return RedirectToAction("Confirmation");
            }
            return View(application);
        }

        [HttpGet]
        public IActionResult Confirmation()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(int applicationId, string token)
        {
            var result = new ApplicationEmailConfirmationResult
            {
                Success = false,
                Title = "Email confirmation failed",
                Message = "The confirmation link is invalid. Please request a new link from HR if needed."
            };

            if (applicationId <= 0 || string.IsNullOrWhiteSpace(token))
            {
                return View("ConfirmEmail", result);
            }

            var application = await _context.JobApplications.FirstOrDefaultAsync(a => a.Id == applicationId && a.Status == ApplicationStatus.Approved);
            if (application == null)
            {
                result.Message = "We could not find an approved application for this link.";
                return View("ConfirmEmail", result);
            }

            if (application.EmailConfirmed)
            {
                result.Success = true;
                result.Title = "Email already confirmed";
                result.Message = "Your email has already been confirmed. You can proceed to register your account.";
            }
            else if (!string.Equals(application.EmailConfirmationToken, token, StringComparison.Ordinal))
            {
                result.Message = "This confirmation link is invalid or has already been used.";
            }
            else
            {
                application.EmailConfirmed = true;
                application.EmailConfirmationToken = null;
                await _context.SaveChangesAsync();

                result.Success = true;
                result.Title = "Email confirmed";
                result.Message = "Thanks for confirming your email. You can now register using the same email and username you applied with.";
            }

            result.RegisterUrl = Url.Page("/Account/Register", pageHandler: null, values: new { area = "Identity" }, protocol: Request.Scheme);
            return View("ConfirmEmail", result);
        }
    }
}
