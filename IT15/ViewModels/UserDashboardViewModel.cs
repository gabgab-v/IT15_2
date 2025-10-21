using IT15.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IT15.ViewModels
{
    public class UserDashboardViewModel
    {
        // Properties for the Main Dashboard (Index page)
        public TodayAttendanceStatus AttendanceStatus { get; set; }
        public LeaveRequest UpcomingLeave { get; set; } = null!;
        public int PendingLeaveRequestsCount { get; set; }
        public int PendingOvertimeRequestsCount { get; set; }
        public decimal ApprovedOvertimeHoursThisMonth { get; set; }

        public int PendingSupplyRequestsCount { get; set; }

        // NEW: Property for leave balance, used on multiple pages
        [Display(Name = "Available Leave Days")]
        public int AvailableLeaveDays { get; set; }

        // Properties for the "Apply for Leave" page
        public LeaveRequest LeaveRequest { get; set; } = null!;

        // Properties for the "My Attendance" (DailyLogs page)
        public IEnumerable<DailyLog> Logs { get; set; } = new List<DailyLog>();
        public bool HasCheckedInToday { get; set; }
        public bool HasCheckedOutToday { get; set; }
        public bool CanCheckOutNow { get; set; }
        public string ScheduledEndTime { get; set; } = string.Empty;
        public int SelectedMonth { get; set; }
        public int SelectedYear { get; set; }
        public string SelectedStatusFilter { get; set; } = "All";
        public List<int> AvailableYears { get; set; } = new List<int>();
        public List<string> AvailableStatusFilters { get; set; } = new List<string>();
        public List<CalendarDayViewModel> CalendarDays { get; set; } = new List<CalendarDayViewModel>();

        // --- NEW PROPERTIES FOR CHARTS AND REPORTS ---
        public int LeaveDaysUsed { get; set; }
        public int LeaveDaysRemaining { get; set; }
        public List<string> SalesChartLabels { get; set; } = new List<string>();
        public List<decimal> SalesChartData { get; set; } = new List<decimal>();
        public List<ActivityLogItem> RecentActivity { get; set; } = new List<ActivityLogItem>();
    }

    // This enum is used by the main dashboard
    public enum TodayAttendanceStatus
    {
        NotCheckedIn,
        CheckedIn,
        CheckedInCannotCheckOut,
        Completed
    }

    public class CalendarDayViewModel
    {
        public DateTime Date { get; set; }
        public bool IsCurrentMonth { get; set; }
        public bool IsToday { get; set; }
        public bool IsWeekend { get; set; }
        public bool IsLeave { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusDescription { get; set; } = string.Empty;
        public DailyLog? Log { get; set; }
        public bool IsFilteredOut { get; set; }
    }
}
