using IT15.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace IT15.ViewModels
{
    public class SalesHistoryViewModel
    {
        public List<CompanyLedger> SalesRecords { get; set; } = new List<CompanyLedger>();
        public List<SelectListItem> Employees { get; set; } = new List<SelectListItem>();

        // Filter properties
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string SelectedEmployeeId { get; set; }
        public string FilterType { get; set; }

        public decimal TotalRevenueForPeriod { get; set; }
        public List<string> SalesOverTimeLabels { get; set; } = new List<string>();
        public List<decimal> SalesOverTimeData { get; set; } = new List<decimal>();
        public List<string> SalesByEmployeeLabels { get; set; } = new List<string>();
        public List<decimal> SalesByEmployeeData { get; set; } = new List<decimal>();
    }
}
