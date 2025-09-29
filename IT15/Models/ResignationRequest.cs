using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15.Models
{
    public enum ResignationStatus { Pending, Approved, Denied }

    public class ResignationRequest
    {
        public int Id { get; set; }

        // THE FIX: The [Required] attribute has been removed.
        // The property remains nullable to pass initial form validation.
        public string? RequestingEmployeeId { get; set; }
        [ForeignKey("RequestingEmployeeId")]
        public IdentityUser? RequestingEmployee { get; set; }

        [Required]
        [Display(Name = "Effective Resignation Date")]
        [DataType(DataType.Date)]
        public DateTime EffectiveDate { get; set; }

        [Required]
        [StringLength(500, MinimumLength = 10)]
        public string Reason { get; set; }

        public ResignationStatus Status { get; set; } = ResignationStatus.Pending;

        public string? ApprovedById { get; set; }
        [ForeignKey("ApprovedById")]
        public IdentityUser? ApprovedBy { get; set; }

        public DateTime DateSubmitted { get; set; }
        public DateTime? DateActioned { get; set; }
    }
}

