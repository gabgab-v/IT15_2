using IT15.Data;
using IT15.Models;
using IT15.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IT15.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context; // Declare both services

        // THE FIX: The constructor must request both services from dependency injection.
        public UserController(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context; // Initialize the DbContext
        }

        // GET: /Admin/User (Shows only ACTIVE users)
        public async Task<IActionResult> Index()
        {
            // Get IDs of all archived users from their profiles
            var archivedUserIds = await _context.UserProfiles
                .Where(p => p.IsArchived)
                .Select(p => p.UserId)
                .ToListAsync();

            // Get all IdentityUsers who are NOT in the archived list
            var users = await _userManager.Users
                .Where(u => !archivedUserIds.Contains(u.Id))
                .ToListAsync();

            var userViewModels = new List<UserViewModel>();
            foreach (var user in users)
            {
                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = await _userManager.GetRolesAsync(user)
                });
            }

            return View(userViewModels);
        }

        // GET: /Admin/User/Archived
        [HttpGet]
        public async Task<IActionResult> Archived()
        {
            var archivedUserIds = await _context.UserProfiles.Where(p => p.IsArchived).Select(p => p.UserId).ToListAsync();
            var users = await _userManager.Users.Where(u => archivedUserIds.Contains(u.Id)).ToListAsync();

            var userViewModels = new List<UserViewModel>();
            foreach (var user in users)
            {
                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = await _userManager.GetRolesAsync(user)
                });
            }

            return View(userViewModels);
        }

        // POST: /Admin/User/Archive/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(string id)
        {
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == id);
            if (userProfile == null)
            {
                userProfile = new UserProfile { UserId = id, IsArchived = true };
                _context.UserProfiles.Add(userProfile);
            }
            else
            {
                userProfile.IsArchived = true;
                _context.UserProfiles.Update(userProfile);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // POST: /Admin/User/Restore/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(string id)
        {
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == id);
            if (userProfile != null)
            {
                userProfile.IsArchived = false;
                _context.UserProfiles.Update(userProfile);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Archived");
        }

        // GET: /Admin/User/Edit/some-guid
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                Roles = await _userManager.GetRolesAsync(user)
            };

            return View(model);
        }

        // POST: /Admin/User/Edit/some-guid
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
            }

            await _userManager.UpdateAsync(user);
            return RedirectToAction("Index");
        }
    }
}

