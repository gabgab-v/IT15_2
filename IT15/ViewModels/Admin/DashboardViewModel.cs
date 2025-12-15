namespace IT15.ViewModels.Admin
{
    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int AttendanceTodayCount { get; set; }
        public int UsersOnLeaveToday { get; set; }
        public int PendingSupplyRequests { get; set; }
        public int PendingLeaveRequests { get; set; }
        public int PendingOvertimeRequests { get; set; }
        public decimal SalesThisMonth { get; set; }
        public decimal ExpensesThisMonth { get; set; }
        public List<string> FinanceChartLabels { get; set; } = new();
        public List<decimal> FinanceIncomeSeries { get; set; } = new();
        public List<decimal> FinanceExpenseSeries { get; set; } = new();
        public List<string> AttendanceChartLabels { get; set; } = new();
        public List<int> AttendanceChartData { get; set; } = new();
        public List<string> LeaveStatusLabels { get; set; } = new();
        public List<int> LeaveStatusData { get; set; } = new();
        public List<string> ApprovalBacklogLabels { get; set; } = new();
        public List<int> ApprovalBacklogData { get; set; } = new();
        public IList<RecentAuditLog> RecentAuditLogs { get; set; } = new List<RecentAuditLog>();
    }

    public class RecentAuditLog
    {
        public string Timestamp { get; set; }
        public string UserName { get; set; }
        public string ActionType { get; set; }
        public string Details { get; set; }
    }
}
