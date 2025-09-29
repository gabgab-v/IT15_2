using IT15.Models;
using System.Collections.Generic;

namespace IT15.ViewModels
{
    // This class will hold the combined data for a single product.
    public class SalesProductViewModel
    {
        public StoreProduct Product { get; set; }
        public int StockLevel { get; set; }
    }

    // This is the main model for the Sales page.
    public class SalesViewModel
    {
        public List<SalesProductViewModel> Products { get; set; } = new List<SalesProductViewModel>();
        public decimal CurrentBalance { get; set; }

        public decimal RevenueMarginPercent { get; set; }
        public List<CompanyLedger> RecentSales { get; set; } = new List<CompanyLedger>();
    }
}
