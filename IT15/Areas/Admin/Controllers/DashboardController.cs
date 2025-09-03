using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OpenBookHRIS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // This ensures only logged-in users can access the dashboard
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}