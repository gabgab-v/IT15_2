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
        public string UserId { get; set; } // Employee who recorded the entry

        [ForeignKey(nameof(UserId))]
        public IdentityUser User { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        /// <summary>
        /// Human readable summary of the transaction.
        /// </summary>
        [Required]
        [MaxLength(512)]
        public string Description { get; set; }

        [Required]
        public LedgerEntryType EntryType { get; set; }

        [Required]
        public LedgerEntryCategory Category { get; set; }

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; }

        /// <summary>
        /// Supplier, customer, or internal party associated with the entry.
        /// </summary>
        [MaxLength(256)]
        public string? Counterparty { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        public int? AccountsReceivableId { get; set; }

        public AccountsReceivable AccountsReceivable { get; set; }

        public int? AccountsPayableId { get; set; }

        public AccountsPayable AccountsPayable { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
