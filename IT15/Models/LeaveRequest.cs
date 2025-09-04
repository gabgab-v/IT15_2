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

        // We remove [Required] because the controller sets this ID, not the form.
        public string RequestingEmployeeId { get; set; }
        [ForeignKey("RequestingEmployeeId")]
        // We make the navigation property nullable ('?') to prevent validation errors.
        public IdentityUser? RequestingEmployee { get; set; }

        public string? ApprovedById { get; set; }
        [ForeignKey("ApprovedById")]
        public IdentityUser? ApprovedBy { get; set; }


        // --- Leave Details ---

        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
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
    }
}

