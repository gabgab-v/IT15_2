using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// IMPORTANT: The namespace must match your project and folder structure.
namespace IT15.Areas.Admin.Controllers
{
    // Designate this controller as part of the "Admin" area.
    [Area("Admin")]
    // Secure this controller so only users with the "Admin" role can access it.
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        // This action handles requests to /Admin/Dashboard or /Admin/Dashboard/Index
        public IActionResult Index()
        {
            // This tells the application to render the corresponding view.
            return View();
        }
    }
}

