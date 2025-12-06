using Microsoft.AspNetCore.Identity;

namespace IT15.ViewModels.HumanResource
{
    public class EmployeePayrollViewModel
    {
        public IdentityUser Employee { get; set; }
        public int DaysPresent { get; set; }
        public int DaysAbsent { get; set; }
        public decimal LeaveHours { get; set; }
        public decimal AbsentHours { get; set; }
        public decimal ApprovedOvertimeHours { get; set; }
        public decimal ActualOvertimeHours { get; set; }
        public decimal OvertimePenaltyHours { get; set; }
        public int WorkingDaysInMonth { get; set; }

        // Calculated Fields
        public decimal BasicSalary { get; set; }
        public decimal OvertimePay { get; set; }
        public decimal DailyRate { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal GrossPay { get; set; }
        public decimal AbsentDeductions { get; set; }
        public decimal SssDeduction { get; set; }
        public decimal PhilHealthDeduction { get; set; }
        public decimal PagIbigDeduction { get; set; }
        public decimal TaxDeduction { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetPay { get; set; }

        public decimal OvertimePenalty { get; set; }
    }
}
