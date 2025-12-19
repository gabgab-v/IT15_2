using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using IT15.Areas.Admin.ViewModels;
using Microsoft.Extensions.Logging;

namespace IT15.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            returnUrl ??= Url.Action("Index", "Dashboard", new { area = "Admin" });

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        _logger.LogInformation("User logged in successfully.");
                        return LocalRedirect(returnUrl);
                    }

                    await _signInManager.SignOutAsync();
                    _logger.LogWarning("Unauthorized login attempt to Admin area for user {Email}", model.Email);
                    ModelState.AddModelError(string.Empty, "You do not have permission to access this area.");
                    return View(model);
                }

                if (result.RequiresTwoFactor)
                {
                    if (user == null || !await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        _logger.LogWarning("Unauthorized login attempt requiring 2FA to Admin area for user {Email}", model.Email);
                        ModelState.AddModelError(string.Empty, "You do not have permission to access this area.");
                        return View(model);
                    }

                    _logger.LogInformation("User requires two-factor authentication.");
                    return RedirectToPage("/Account/LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe, Area = "Identity" });
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("/Account/Lockout", new { Area = "Identity" });
                }
                else
                {
                    _logger.LogWarning("Invalid login attempt for user {Email}", model.Email);
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            _logger.LogWarning("Login failed due to invalid model state.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(Login), "Account", new { area = "Admin" });
        }
    }
}

