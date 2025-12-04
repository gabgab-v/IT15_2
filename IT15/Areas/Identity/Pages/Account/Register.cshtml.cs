// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using IT15.Services;
using IT15.Data; // Required for ApplicationDbContext
using IT15.Models; // Required for ApplicationStatus
using Microsoft.EntityFrameworkCore; // Required for .FirstOrDefaultAsync()

namespace IT15.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IAuditService _auditService;
        private readonly ApplicationDbContext _context;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IAuditService auditService,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _auditService = auditService;
            _context = context;

        }

        [BindProperty]
        public InputModel Input { get; set; }
        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Username")]
            public string Username { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            // THE CHANGE: Added a property for the country code dropdown
            [Display(Name = "Country Code")]
            public string CountryCode { get; set; } = "+63"; // Default to Philippines

            // THE CHANGE: Updated validation to be more generic for international numbers
            [Phone]
            [Display(Name = "Phone Number")]
            [RegularExpression(@"^\d{7,15}$", ErrorMessage = "Please enter a valid phone number (7-15 digits).")]
            public string PhoneNumber { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 12)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            var approvedApplication = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.Email == Input.Email && a.Username == Input.Username && a.Status == ApplicationStatus.Approved);

            if (approvedApplication == null)
            {
                ModelState.AddModelError(string.Empty, "Your application has not been approved yet, or the details do not match an approved application.");
            }
            else if (!approvedApplication.EmailConfirmed)
            {
                ModelState.AddModelError(string.Empty, "Please confirm the approval email we sent before registering your account.");
            }

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                // THE CHANGE: Combine the country code and phone number before saving.
                if (!string.IsNullOrEmpty(Input.PhoneNumber))
                {
                    user.PhoneNumber = Input.CountryCode + Input.PhoneNumber;
                }

                user.EmailConfirmed = true;
                await _userStore.SetUserNameAsync(user, Input.Username, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");
                    await _userManager.AddToRoleAsync(user, "User");
                    await _auditService.LogAsync(user.Id, user.UserName, "User Registration Success", $"New user '{user.UserName}' registered successfully.");

                    // Do not auto-sign the user in after registration; send them to the homepage instead.
                    return LocalRedirect("~/");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                    await _auditService.LogAsync(null, Input.Username, "User Registration Failure", $"Failed registration attempt for '{Input.Username}'. Reason: {error.Description}");
                }
            }
            return Page();
        }

        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
                    $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}

