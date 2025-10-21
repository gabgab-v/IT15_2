using IT15.Models;
using IT15.Services;
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
        public decimal TotalSupplyExpense { get; set; }
        public decimal TotalDeliveryFeeExpense { get; set; }
        public decimal TotalOperationalExpense { get; set; } // Other costs like rent, utilities

        // Summary
        public decimal TotalExpenses => TotalPayrollExpense + TotalSupplyExpense + TotalDeliveryFeeExpense + TotalOperationalExpense;
        public decimal NetIncome => TotalRevenue - TotalExpenses;

        // For Detailed Log
        public List<CompanyLedger> Transactions { get; set; } = new List<CompanyLedger>();

        public ReceivablesSummary ReceivablesSummary { get; set; } = new ReceivablesSummary();
        public PayablesSummary PayablesSummary { get; set; } = new PayablesSummary();
        public RevenueAnalysis RevenueAnalysis { get; set; } = new RevenueAnalysis();
    }
}
