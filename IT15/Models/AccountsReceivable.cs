using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace IT15.Models
{
    public class AccountsReceivable
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string CustomerName { get; set; }

        [MaxLength(100)]
        public string ReferenceNumber { get; set; }

        [Required]
        public DateTime InvoiceDate { get; set; }

        public DateTime? DueDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal InvoiceAmount { get; set; }

        [Required]
        public LedgerEntryCategory RevenueCategory { get; set; } = LedgerEntryCategory.Sales;

        [Required]
        public ReceivableStatus Status { get; set; } = ReceivableStatus.Pending;

        public string? Notes { get; set; }

        public ICollection<CompanyLedger> LedgerEntries { get; set; } = new List<CompanyLedger>();

        [NotMapped]
        public decimal AmountCollected =>
            LedgerEntries?.Where(e => e.EntryType == LedgerEntryType.Income).Sum(e => e.Amount) ?? 0m;

        [NotMapped]
        public decimal OutstandingBalance => InvoiceAmount - AmountCollected;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
