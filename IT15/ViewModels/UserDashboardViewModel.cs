using IT15.Models;
using System.Collections.Generic;

namespace IT15.ViewModels
{
    public class UserDashboardViewModel
    {
        // Properties for the Main Dashboard (Index page)
        public TodayAttendanceStatus AttendanceStatus { get; set; }
        public LeaveRequest UpcomingLeave { get; set; }
        public int PendingLeaveRequestsCount { get; set; }
        public int PendingOvertimeRequestsCount { get; set; }
        public decimal ApprovedOvertimeHoursThisMonth { get; set; }

        // Properties for the "My Attendance" (DailyLogs page)
        public IEnumerable<DailyLog> Logs { get; set; }
        public bool HasCheckedInToday { get; set; }
        public bool HasCheckedOutToday { get; set; }
        public bool CanCheckOutNow { get; set; }
        public string ScheduledEndTime { get; set; }
    }

    // This enum is used by the main dashboard
    public enum TodayAttendanceStatus
    {
        NotCheckedIn,
        CheckedIn,
        CheckedInCannotCheckOut,
        Completed
    }
}

