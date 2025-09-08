using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IT15.Models
{
    public class Payroll
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Payroll Month")]
        [DataType(DataType.Date)]
        public DateTime PayrollMonth { get; set; }

        [Required]
        [Display(Name = "Date Generated")]
        public DateTime DateGenerated { get; set; }

        public ICollection<PaySlip> PaySlips { get; set; } = new List<PaySlip>();
    }
}

