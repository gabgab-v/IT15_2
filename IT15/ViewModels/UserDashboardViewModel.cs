using IT15.Models;

namespace IT15.ViewModels
{
    public class UserDashboardViewModel
    {
        public TodayAttendanceStatus AttendanceStatus { get; set; }
        public LeaveRequest? UpcomingLeave { get; set; }
        public int PendingLeaveRequestsCount { get; set; }
        public int PendingOvertimeRequestsCount { get; set; }
        public decimal ApprovedOvertimeHoursThisMonth { get; set; }
    }

    public enum TodayAttendanceStatus
    {
        NotCheckedIn,
        CheckedIn,
        Completed
    }
}
