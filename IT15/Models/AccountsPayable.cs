using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace IT15.Models
{
    public class AccountsPayable
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string SupplierName { get; set; }

        [MaxLength(100)]
        public string ReferenceNumber { get; set; }

        [Required]
        public DateTime BillDate { get; set; }

        public DateTime? DueDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal BillAmount { get; set; }

        [Required]
        public LedgerEntryCategory ExpenseCategory { get; set; } = LedgerEntryCategory.Supplies;

        [Required]
        public PayableStatus Status { get; set; } = PayableStatus.Pending;

        public string? Notes { get; set; }

        public ICollection<CompanyLedger> LedgerEntries { get; set; } = new List<CompanyLedger>();

        [NotMapped]
        public decimal AmountDisbursed =>
            LedgerEntries?.Where(e => e.EntryType == LedgerEntryType.Expense).Sum(e => Math.Abs(e.Amount)) ?? 0m;

        [NotMapped]
        public decimal OutstandingBalance => BillAmount - AmountDisbursed;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
