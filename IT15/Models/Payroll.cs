using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15.Models
{
    public enum PayrollStatus
    {
        [Display(Name = "Pending Approval")]
        PendingApproval,
        [Display(Name = "Budget Approved")]
        BudgetApproved,
        Completed
    }
    public class Payroll
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Payroll Month")]
        [DataType(DataType.Date)]
        public DateTime PayrollMonth { get; set; }

        [Required]
        [Display(Name = "Date Generated")]
        public DateTime DateGenerated { get; set; }

        public PayrollStatus Status { get; set; }

        public string? ApprovedById { get; set; }
        [ForeignKey("ApprovedById")]
        public IdentityUser? ApprovedBy { get; set; }
        public DateTime? DateApproved { get; set; }

        public bool IsArchived { get; set; } = false;

        public ICollection<PaySlip> PaySlips { get; set; } = new List<PaySlip>();
    }
}

