using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15.Models
{
    public class CompanyLedger
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // The employee who recorded the sale
        [ForeignKey("UserId")]
        public IdentityUser User { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }
    }
} 