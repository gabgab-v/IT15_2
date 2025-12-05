using IT15.Models;
using IT15.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IT15.ViewModels.Accounting
{
    public class PayablesIndexViewModel
    {
        public PayablesSummary Summary { get; set; } = new PayablesSummary();
        public List<PayableListItemViewModel> Items { get; set; } = new();
    }

    public class PayableListItemViewModel
    {
        public int Id { get; set; }
        public string SupplierName { get; set; }
        public string ReferenceNumber { get; set; }
        public DateTime BillDate { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal BillAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal OutstandingAmount { get; set; }
        public PayableStatus Status { get; set; }
        public LedgerEntryCategory ExpenseCategory { get; set; }
    }

    public class PayableFormViewModel
    {
        public int? Id { get; set; }

        [Required]
        [Display(Name = "Supplier Name")]
        public string SupplierName { get; set; }

        [Required]
        [Display(Name = "Reference Number")]
        public string ReferenceNumber { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Bill Date")]
        public DateTime BillDate { get; set; } = DateTime.UtcNow.Date;

        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "Bill Amount")]
        public decimal BillAmount { get; set; }

        [Display(Name = "Expense Category")]
        public LedgerEntryCategory ExpenseCategory { get; set; } = LedgerEntryCategory.Operations;

        public string? Notes { get; set; }

        public IEnumerable<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();
    }

    public class PayablePaymentViewModel
    {
        public int PayableId { get; set; }
        public string SupplierName { get; set; }
        public string ReferenceNumber { get; set; }
        public decimal OutstandingAmount { get; set; }

        [Required]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "Payment amount must be greater than zero.")]
        [DataType(DataType.Currency)]
        [Display(Name = "Payment Amount")]
        public decimal Amount { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Payment Date")]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow.Date;

        [Display(Name = "Ledger Description")]
        public string Description { get; set; }
    }
}
