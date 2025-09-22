using System.ComponentModel.DataAnnotations.Schema;

namespace IT15.Models
{
    public class Supply
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int SupplierId { get; set; }
        [ForeignKey("SupplierId")]
        public Supplier Supplier { get; set; }
        public int StockLevel { get; set; } = 0;
    }
}

