using System.Collections.Generic;
using IT15.Models;
using IT15.Services;

namespace IT15.ViewModels.Accounting
{
    public class AccountingDashboardViewModel
    {
        public int PayrollsPendingApprovalCount { get; set; }
        public decimal TotalPendingPayrollValue { get; set; }
        public int PayrollsApprovedThisMonth { get; set; }
        public List<Holiday> UpcomingHolidays { get; set; } = new List<Holiday>();

        public decimal AvailableFunds { get; set; }
        public FinancialSnapshot FinancialSnapshot { get; set; } = new FinancialSnapshot();
        public ReceivablesSummary ReceivablesSummary { get; set; } = new ReceivablesSummary();
        public PayablesSummary PayablesSummary { get; set; } = new PayablesSummary();
        public RevenueAnalysis RevenueAnalysis { get; set; } = new RevenueAnalysis();
    }
}
