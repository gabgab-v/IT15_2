using IT15.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace IT15.ViewModels
{
    public class SupplyCatalogViewModel
    {
        public IEnumerable<StoreProduct> Products { get; set; }
        public List<SelectListItem> DeliveryServices { get; set; } = new List<SelectListItem>();
    }
}

