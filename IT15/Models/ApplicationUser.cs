using Microsoft.AspNetCore.Identity;

namespace IT15.Models
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsArchived { get; set; } = false; // Default to not archived
    }
}