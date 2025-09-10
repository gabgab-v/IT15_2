using Microsoft.AspNetCore.Identity;
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

        // THE FIX: Add these properties to hold role information

        // This will hold ALL roles from the database for display in the view
        public List<IdentityRole> AllRoles { get; set; } = new List<IdentityRole>();

        // This will hold the names of the roles the user is currently in
        public IList<string> UserRoles { get; set; } = new List<string>();

        // This property will receive the list of selected roles from the form submission
        public List<string> SelectedRoles { get; set; } = new List<string>();
    }
}

