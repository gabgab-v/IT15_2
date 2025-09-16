using IT15.Models;
using System.Collections.Generic;

namespace IT15.ViewModels.HumanResource
{
    public class PayrollReviewViewModel
    {
        public Payroll Payroll { get; set; }
        public List<PaySlip> PaySlips { get; set; } = new List<PaySlip>();
        public decimal TotalNetPay { get; set; }
    }
}
