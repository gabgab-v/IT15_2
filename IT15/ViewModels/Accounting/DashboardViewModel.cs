using IT15.Models;

namespace IT15.ViewModels.Accounting
{
    public class AccountingDashboardViewModel
    {
        public int PayrollsPendingApprovalCount { get; set; }
        public decimal TotalPendingPayrollValue { get; set; }
        public int PayrollsApprovedThisMonth { get; set; }
        public List<Holiday> UpcomingHolidays { get; set; } = new List<Holiday>();

        public decimal AvailableFunds { get; set; }
    }
}
