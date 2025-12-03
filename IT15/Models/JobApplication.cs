using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15.Models
{
    public enum ApplicationStatus { Pending, Approved, Denied }

    public class JobApplication
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        // This property is required for the form but will NOT be saved to the database.
        [NotMapped]
        [Display(Name = "Country Code")]
        public string CountryCode { get; set; } = "+63";

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        public bool EmailConfirmed { get; set; }

        public string EmailConfirmationToken { get; set; }

        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

        public DateTime DateApplied { get; set; }
    }
}
