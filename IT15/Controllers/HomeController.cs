using IT15.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace IT15.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }
                if (User.IsInRole("HumanResource"))
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "HumanResource" });
                }
                if (User.IsInRole("Accounting"))
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Accounting" });
                }

                // Default authenticated landing page for standard users
                return RedirectToAction("Index", "UserDashboard");
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
