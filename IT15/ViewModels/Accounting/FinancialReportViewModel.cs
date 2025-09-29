using IT15.Models;
using System;
using System.Collections.Generic;

namespace IT15.ViewModels.Accounting
{
    public class FinancialReportViewModel
    {
        public DateTime ReportMonth { get; set; }

        // Income
        public decimal TotalRevenue { get; set; }

        // Expenses (Broken Down)
        public decimal TotalPayrollExpense { get; set; }
        public decimal TotalDeliveryFeeExpense { get; set; }
        public decimal TotalOperationalExpense { get; set; } // Other costs like rent, utilities

        // Summary
        public decimal TotalExpenses => TotalPayrollExpense + TotalDeliveryFeeExpense + TotalOperationalExpense;
        public decimal NetIncome => TotalRevenue + TotalExpenses; // Expenses are negative

        // For Detailed Log
        public List<CompanyLedger> Transactions { get; set; } = new List<CompanyLedger>();
    }
}

