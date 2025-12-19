using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using IT15.Services;
using System.Threading.Tasks;

namespace IT15.Areas.HumanResource.Controllers
{
    [Area("HumanResource")]
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AccountController> _logger;
        private readonly IAuditService _auditService;

        public AccountController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, ILogger<AccountController> logger, IAuditService auditService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _auditService = auditService;
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
            returnUrl ??= Url.Action("Index", "Dashboard", new { area = "HumanResource" });

            if (ModelState.IsValid)
            {
                // First, check the password and get the sign-in result.
                var user = await _userManager.FindByEmailAsync(model.Email);
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    
                    // Now, verify the role after a successful password sign-in.
                    if (user != null && await _userManager.IsInRoleAsync(user, "HumanResource"))
                    {
                        _logger.LogInformation("HR user logged in.");
                        await _auditService.LogAsync(user.Id, user.UserName, "HR Login Success", $"User '{user.UserName}' logged into the HR Panel.");
                        return LocalRedirect(returnUrl);
                    }
                    else
                    {
                        // If the role is wrong, sign them out immediately.
                        await _signInManager.SignOutAsync();
                        if (user != null)
                        {
                            await _auditService.LogAsync(user.Id, user.UserName, "HR Login Failure", $"User '{user.UserName}' failed to log into HR Panel (Insufficient Permissions).");
                        }
                        ModelState.AddModelError(string.Empty, "You do not have permission to access this area.");
                        return View(model);
                    }
                }
                if (result.RequiresTwoFactor)
                {
                    if (user == null || !await _userManager.IsInRoleAsync(user, "HumanResource"))
                    {
                        await _signInManager.SignOutAsync();
                        _logger.LogWarning("Unauthorized 2FA login attempt for HR Panel by user {Email}", model.Email);
                        if (user != null)
                        {
                            await _auditService.LogAsync(user.Id, user.UserName, "HR Login Failure", $"User '{user.UserName}' failed to log into HR Panel (Insufficient Permissions).");
                        }
                        ModelState.AddModelError(string.Empty, "You do not have permission to access this area.");
                        return View(model);
                    }

                    _logger.LogInformation("User requires two-factor authentication.");
                    if (user != null)
                    {
                        await _auditService.LogAsync(user.Id, user.UserName, "HR Login 2FA Required", $"User '{user.UserName}' login to HR Panel requires 2FA.");
                    }
                    return RedirectToPage("/Account/LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe, Area = "Identity" });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    if (user != null)
                    {
                        await _auditService.LogAsync(user.Id, user.UserName, "HR Account Locked", $"User account '{user.UserName}' is locked out from HR Panel.");
                    }
                    return RedirectToPage("/Account/Lockout", new { Area = "Identity" });
                }

                if (user != null)
                {
                    await _auditService.LogAsync(user.Id, user.UserName, "HR Login Failure", $"Invalid password attempt for user '{user.UserName}' on HR Panel.");
                }
                else
                {
                    await _auditService.LogAsync("N/A", model.Email, "HR Login Failure", $"Failed login attempt for non-existent user '{model.Email}' on HR Panel.");
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            await _signInManager.SignOutAsync();
            _logger.LogInformation("HR user logged out.");
            if (user != null)
            {
                await _auditService.LogAsync(user.Id, user.UserName, "HR Logout", $"User '{user.UserName}' logged out from the HR Panel.");
            }
            return RedirectToAction(nameof(Login), "Account", new { area = "HumanResource" });
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

