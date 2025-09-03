using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

// IMPORTANT: Update this namespace to match your project's name
namespace YourProjectName.Controllers
{
    // This attribute ensures only authenticated users can access this controller's actions.
    [Authorize]
    public class UserDashboardController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;

        public UserDashboardController(SignInManager<IdentityUser> signInManager)
        {
            _signInManager = signInManager;
        }

        // GET: /UserDashboard/Index
        public IActionResult Index()
        {
            // The view will display a dashboard tailored for a regular user.
            return View();
        }

        // POST action for logging out from the user dashboard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            // Redirect to the home page after logging out
            return RedirectToAction("Index", "Home");
        }
    }
}
