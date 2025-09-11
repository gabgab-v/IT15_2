using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation; // Add this using directive
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IT15.ViewModels.Admin
{
    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "New Password (optional)")]
        public string? NewPassword { get; set; }

        // THE FIX: Add the [ValidateNever] attribute.
        // This tells the application not to require a value for this property
        // when the form is submitted.
        [ValidateNever]
        public string UserRole { get; set; }

        // This property receives the single role selected from the radio buttons.
        public string SelectedRole { get; set; }

        // This holds all possible roles to build the radio button list.
        public List<IdentityRole> AllRoles { get; set; } = new List<IdentityRole>();
    }
}

