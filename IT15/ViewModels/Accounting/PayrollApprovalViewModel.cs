using IT15.Models;

namespace IT15.ViewModels.Accounting
{
    public class PayrollApprovalViewModel
    {
        public Payroll Payroll { get; set; }
        public decimal AvailableFundsForPeriod { get; set; }
    }
}
