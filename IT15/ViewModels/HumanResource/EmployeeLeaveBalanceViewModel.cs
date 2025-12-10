using Microsoft.AspNetCore.Identity;

namespace IT15.ViewModels.HumanResource
{
    public class EmployeeLeaveBalanceViewModel
    {
        public IdentityUser Employee { get; set; }
        public int CurrentLeaveBalance { get; set; }
        public decimal CurrentLeaveHours => CurrentLeaveBalance * 8;
    }
}
