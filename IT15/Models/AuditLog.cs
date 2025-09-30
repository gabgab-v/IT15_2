using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [Required]
        public string? UserId { get; set; }
        [ForeignKey("UserId")]
        public IdentityUser? User { get; set; }

        // Denormalized for easier display without extra queries
        public string UserName { get; set; }

        [Required]
        public string ActionType { get; set; } // e.g., "User Login", "Payroll Approved"

        [Required]
        public string Details { get; set; } // e.g., "User 'admin@openbook.com' logged in successfully."

        [Required]
        public DateTime Timestamp { get; set; }
    }
}

