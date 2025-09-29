using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using IT15.Services; // Add this to use the Audit Service

namespace IT15.Areas.Accounting.Controllers
{
    [Area("Accounting")]
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AccountController> _logger;
        private readonly IAuditService _auditService; // Inject the audit service

        public AccountController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ILogger<AccountController> logger,
            IAuditService auditService) // Request the service
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _auditService = auditService; // Initialize the service
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            returnUrl ??= Url.Action("Index", "Dashboard", new { area = "Accounting" });

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    if (user != null && (await _userManager.IsInRoleAsync(user, "Accounting") || await _userManager.IsInRoleAsync(user, "Admin")))
                    {
                        _logger.LogInformation("Accounting user logged in.");
                        // --- AUDIT LOG ---
                        await _auditService.LogAsync(user.Id, user.UserName, "Accounting Login Success", $"User '{user.UserName}' logged into the Accounting Panel.");
                        return LocalRedirect(returnUrl);
                    }
                    else
                    {
                        await _signInManager.SignOutAsync();
                        // --- AUDIT LOG ---
                        if (user != null)
                        {
                            await _auditService.LogAsync(user.Id, user.UserName, "Accounting Login Failure", $"User '{user.UserName}' failed to log into Accounting Panel (Insufficient Permissions).");
                        }
                        ModelState.AddModelError(string.Empty, "You do not have permission to access this area.");
                        return View(model);
                    }
                }
                if (result.RequiresTwoFactor)
                {
                    _logger.LogInformation("User requires two-factor authentication.");
                    // --- AUDIT LOG ---
                    if (user != null)
                    {
                        await _auditService.LogAsync(user.Id, user.UserName, "Accounting Login 2FA Required", $"User '{user.UserName}' login to Accounting Panel requires 2FA.");
                    }
                    return RedirectToPage("/Account/LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe, Area = "Identity" });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    // --- AUDIT LOG ---
                    if (user != null)
                    {
                        await _auditService.LogAsync(user.Id, user.UserName, "Accounting Account Locked", $"User account '{user.UserName}' is locked out from Accounting Panel.");
                    }
                    return RedirectToPage("/Account/Lockout", new { Area = "Identity" });
                }

                // --- AUDIT LOG for general failure ---
                if (user != null)
                {
                    await _auditService.LogAsync(user.Id, user.UserName, "Accounting Login Failure", $"Invalid password attempt for user '{user.UserName}' on Accounting Panel.");
                }
                else
                {
                    await _auditService.LogAsync("N/A", model.Email, "Accounting Login Failure", $"Failed login attempt for non-existent user '{model.Email}' on Accounting Panel.");
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt or insufficient permissions.");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Accounting user logged out.");

            // --- AUDIT LOG ---
            if (user != null)
            {
                await _auditService.LogAsync(user.Id, user.UserName, "Accounting Logout", $"User '{user.UserName}' logged out from the Accounting Panel.");
            }

            return RedirectToAction(nameof(Login), "Account", new { area = "Accounting" });
        }
    }

    public class LoginViewModel
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
}

