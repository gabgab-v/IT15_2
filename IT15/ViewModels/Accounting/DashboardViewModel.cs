namespace IT15.ViewModels.Accounting
{
    public class AccountingDashboardViewModel
    {
        public int PayrollsPendingApprovalCount { get; set; }
        public decimal TotalPendingPayrollValue { get; set; }
        public int PayrollsApprovedThisMonth { get; set; }
    }
}
