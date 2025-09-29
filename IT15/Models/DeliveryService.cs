using System.ComponentModel.DataAnnotations.Schema;

namespace IT15.Models
{
    public class DeliveryService
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Fee { get; set; }
    }
}

