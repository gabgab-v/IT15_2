using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15.Models
{
    public class UserProfile
    {
        [Key] // Primary Key for this table
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // Foreign Key to AspNetUsers table
        [ForeignKey("UserId")]
        public IdentityUser User { get; set; }

        public bool IsArchived { get; set; } = false;

        public int LeaveBalance { get; set; } = 3;
    }
}