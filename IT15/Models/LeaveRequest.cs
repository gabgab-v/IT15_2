using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15.Models
{
    public enum LeaveRequestStatus
    {
        Pending,
        Approved,
        Denied
    }

    public class LeaveRequest
    {
        public int Id { get; set; }

        // --- User and Approval Info ---

        // THE FIX: Make the string property nullable to remove the implicit [Required] attribute.
        public string? RequestingEmployeeId { get; set; }
        [ForeignKey("RequestingEmployeeId")]
        public IdentityUser? RequestingEmployee { get; set; }

        public string? ApprovedById { get; set; }
        [ForeignKey("ApprovedById")]
        public IdentityUser? ApprovedBy { get; set; }


        // --- Leave Details ---

        [Required]
        [Display(Name = "Start")]
        [DataType(DataType.DateTime)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End")]
        [DataType(DataType.DateTime)]
        public DateTime EndDate { get; set; }

        [Required]
        [StringLength(250, ErrorMessage = "The reason must be less than 250 characters.")]
        public string Reason { get; set; }


        // --- Timestamps and Status ---

        [DataType(DataType.Date)]
        public DateTime DateRequested { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateActioned { get; set; }

        public LeaveRequestStatus Status { get; set; }

        [NotMapped]
        public decimal LeaveHours => Math.Max(0, (decimal)(EndDate - StartDate).TotalHours);
    }
}



