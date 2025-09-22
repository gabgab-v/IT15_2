using IT15.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Drawing;
using System.Text;

namespace IT15.Models
{
    public class PaySlip
    {
        public int Id { get; set; }

        [Required]
        public int PayrollId { get; set; }
        [ForeignKey("PayrollId")]
        public Payroll Payroll { get; set; }

        [Required]
        public string EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public IdentityUser Employee { get; set; }

        public int DaysAbsent { get; set; }

        // Earnings
        [Column(TypeName = "decimal(18, 2)")]
        public decimal BasicSalary { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal OvertimePay { get; set; }
        // Deductions
        [Column(TypeName = "decimal(18, 2)")]
        public decimal AbsentDeductions { get; set; }

        // NEW: Property to store overtime penalties
        [Column(TypeName = "decimal(18, 2)")]
        public decimal OvertimePenalty { get; set; }


        [Column(TypeName = "decimal(18, 2)")]
        public decimal SSSDeduction { get; set; } // Social Security System
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PhilHealthDeduction { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PagIBIGDeduction { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TaxDeduction { get; set; }

        // Summary
        [Column(TypeName = "decimal(18, 2)")]
        public decimal GrossPay { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalDeductions { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal NetPay { get; set; }
    }
}

