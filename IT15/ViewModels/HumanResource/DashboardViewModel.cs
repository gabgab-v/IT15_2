using IT15.Models;

namespace IT15.ViewModels.HumanResource
{
    public class HumanResourceDashboardViewModel
    {
        public int PendingLeaveRequestsCount { get; set; }
        public int EmployeesOnLeaveToday { get; set; }
        public int PayrollsPendingApprovalCount { get; set; }

        // Total number of active employees
        public List<string> AttendanceChartLabels { get; set; } = new List<string>();
        public List<int> AttendanceChartData { get; set; } = new List<int>();

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public List<Holiday> UpcomingHolidays { get; set; } = new List<Holiday>();
    }
}
