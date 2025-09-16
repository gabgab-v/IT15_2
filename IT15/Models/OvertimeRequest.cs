using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15.Models
{
    public class OvertimeRequest
    {
        public int Id { get; set; }

        // THE FIX: The RequestingEmployeeId and its navigation property are now nullable.
        // This prevents the validation system from requiring them when the form is submitted.
        // Your controller will set the actual value before saving.
        public string? RequestingEmployeeId { get; set; }
        [ForeignKey("RequestingEmployeeId")]
        public IdentityUser? RequestingEmployee { get; set; }

        [Required]
        [Display(Name = "Date of Overtime")]
        [DataType(DataType.Date)]
        public DateTime OvertimeDate { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Display(Name = "End Time")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [NotMapped]
        public decimal TotalHours => (decimal)(EndTime - StartTime).TotalHours;

        [Required]
        [StringLength(250)]
        public string Reason { get; set; }

        public OvertimeStatus Status { get; set; } = OvertimeStatus.PendingApproval;

        public string? ApprovedById { get; set; }
        [ForeignKey("ApprovedById")]
        public IdentityUser? ApprovedBy { get; set; }

        public DateTime DateRequested { get; set; }
        public DateTime? DateActioned { get; set; }
    }
}