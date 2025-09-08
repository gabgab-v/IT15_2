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

        // A list of all roles the user currently has
        public IList<string> Roles { get; set; } = new List<string>();

        // A property to set a new password for the user
        [DataType(DataType.Password)]
        [Display(Name = "New Password (optional)")]
        public string? NewPassword { get; set; }
    }
}
