// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using IT15.Services;

namespace IT15.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly IAuditService _auditService;


        public LoginModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, ILogger<LoginModel> logger, IAuditService auditService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _auditService = auditService;
        }

        [BindProperty]
        public InputModel Input { get; set; }
        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        // THE CHANGE: The InputModel now accepts a single "Login" string.
        public class InputModel
        {
            [Required]
            [Display(Name = "Username or Email")]
            public string Login { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/UserDashboard");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // THE CHANGE: This new logic handles both username and email logins.
                var userName = Input.Login;
                // Check if the input looks like an email address.
                if (Input.Login.Contains("@"))
                {
                    var user = await _userManager.FindByEmailAsync(Input.Login);
                    if (user != null)
                    {
                        // If we found a user by email, use their actual UserName for the login attempt.
                        userName = user.UserName;
                    }
                }

                var result = await _signInManager.PasswordSignInAsync(userName, Input.Password, Input.RememberMe, lockoutOnFailure: true);
                var userAttemptingLogin = await _userManager.FindByNameAsync(userName);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    await _auditService.LogAsync(userAttemptingLogin.Id, userName, "User Login Success", $"User '{userName}' logged in successfully.");
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    await _auditService.LogAsync(userAttemptingLogin.Id, userName, "User Login 2FA Required", $"User '{userName}' login requires 2FA.");
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    await _auditService.LogAsync(userAttemptingLogin.Id, userName, "User Account Locked", $"User account '{userName}' is locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    if (userAttemptingLogin != null)
                    {
                        await _auditService.LogAsync(userAttemptingLogin.Id, userName, "User Login Failure", $"Invalid password attempt for user '{userName}'.");
                    }
                    else
                    {
                        await _auditService.LogAsync("N/A", userName, "User Login Failure", $"Failed login attempt for non-existent user '{userName}'.");
                    }
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");

                    return Page();
                }
            }

            return Page();
        }
    }
}

