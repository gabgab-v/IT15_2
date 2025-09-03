using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

// Define the namespace to match your project structure
namespace YourProjectName.Areas.Admin.Controllers // <-- IMPORTANT: Change YourProjectName
{
    // Designate this controller as part of the "Admin" area
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<AccountController> _logger;

        // Constructor to inject Identity services
        public AccountController(SignInManager<IdentityUser> signInManager, ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        // A model to hold the data from the login form
        public class LoginInputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        // GET Action: /Admin/Account/Login
        // This method displays the login form.
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            // If the user is already authenticated, redirect them to the admin dashboard
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST Action: /Admin/Account/Login
        // This method processes the submitted login form.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginInputModel input, string returnUrl = null)
        {
            // Set a default redirect URL if one isn't provided
            returnUrl ??= Url.Action("Index", "Dashboard");

            if (ModelState.IsValid)
            {
                // Attempt to sign the user in using their password.
                // The last parameter `lockoutOnFailure` is set to false to prevent account lockouts from this form.
                var result = await _signInManager.PasswordSignInAsync(input.Email, input.Password, input.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Admin user logged in.");
                    return LocalRedirect(returnUrl);
                }
                else
                {
                    // If login fails, add an error message and redisplay the form.
                    ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your email and password.");
                    return View(input);
                }
            }

            // If the model state is invalid, redisplay the form with validation errors.
            return View(input);
        }

        // POST Action: /Admin/Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            // Redirect to the admin login page after logging out
            return RedirectToAction(nameof(Login));
        }
    }
}
