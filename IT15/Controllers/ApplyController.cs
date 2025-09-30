using IT15.Data;
using IT15.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

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

                application.DateApplied = DateTime.Now;
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
    }
}

