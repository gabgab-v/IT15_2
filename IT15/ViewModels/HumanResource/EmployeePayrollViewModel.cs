using Microsoft.AspNetCore.Identity;

namespace IT15.ViewModels.HumanResource
{
    public class EmployeePayrollViewModel
    {
        public IdentityUser Employee { get; set; }
        public int DaysPresent { get; set; }
        public int DaysAbsent { get; set; }
        public decimal ApprovedOvertimeHours { get; set; }

        // Calculated Fields
        public decimal BasicSalary { get; set; }
        public decimal OvertimePay { get; set; }
        public decimal GrossPay { get; set; }
        public decimal AbsentDeductions { get; set; }
        public decimal SssDeduction { get; set; }
        public decimal PhilHealthDeduction { get; set; }
        public decimal PagIbigDeduction { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetPay { get; set; }

        public decimal OvertimePenalty { get; set; }
    }
}
