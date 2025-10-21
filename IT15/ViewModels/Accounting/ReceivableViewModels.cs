using IT15.Models;
using IT15.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IT15.ViewModels.Accounting
{
    public class ReceivablesIndexViewModel
    {
        public ReceivablesSummary Summary { get; set; } = new ReceivablesSummary();
        public List<ReceivableListItemViewModel> Items { get; set; } = new();
    }

    public class ReceivableListItemViewModel
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public string ReferenceNumber { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal InvoiceAmount { get; set; }
        public decimal CollectedAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
        public ReceivableStatus Status { get; set; }
        public LedgerEntryCategory Category { get; set; }
    }

    public class ReceivableFormViewModel
    {
        public int? Id { get; set; }

        [Required]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; }

        [Required]
        [Display(Name = "Reference Number")]
        public string ReferenceNumber { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Invoice Date")]
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow.Date;

        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "Invoice Amount")]
        public decimal InvoiceAmount { get; set; }

        [Display(Name = "Revenue Category")]
        public LedgerEntryCategory RevenueCategory { get; set; } = LedgerEntryCategory.Sales;

        public string? Notes { get; set; }

        public IEnumerable<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();
    }

    public class ReceivablePaymentViewModel
    {
        public int ReceivableId { get; set; }
        public string CustomerName { get; set; }
        public string ReferenceNumber { get; set; }
        public decimal OutstandingAmount { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        [DataType(DataType.Currency)]
        [Display(Name = "Collection Amount")]
        public decimal Amount { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Collection Date")]
        public DateTime CollectionDate { get; set; } = DateTime.UtcNow.Date;

        [Display(Name = "Ledger Description")]
        public string Description { get; set; }
    }
}
