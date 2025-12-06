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
