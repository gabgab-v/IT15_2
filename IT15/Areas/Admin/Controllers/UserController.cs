using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT15.ViewModels.Admin; // Use the new ViewModel
using Microsoft.AspNetCore.Authorization;

namespace IT15.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // Secure this controller
    public class UserController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public UserController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: /Admin/User
        public async Task<IActionResult> Index()
        {
            // Get all users from the database
            var users = await _userManager.Users.ToListAsync();

            var userViewModels = new List<UserViewModel>();

            foreach (var user in users)
            {
                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed,
                    // Get the list of roles for each user
                    Roles = await _userManager.GetRolesAsync(user)
                });
            }

            // Pass the list of ViewModels to the view
            return View(userViewModels);
        }
    }
}
