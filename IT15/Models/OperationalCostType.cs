using System.ComponentModel.DataAnnotations;

namespace IT15.Models
{
    public enum OperationalCostType
    {
        Rent,
        Water,
        Electricity,
        Internet,
        Marketing,
        [Display(Name = "Office Supplies")]
        OfficeSupplies,
        Other
    }
}
