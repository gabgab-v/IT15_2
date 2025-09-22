using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15.Models
{
    public enum ProductRequestStatus { Pending, Approved, Denied }

    public class ProductRequest
    {
        public int Id { get; set; }

        [Required]
        public string RequestingEmployeeId { get; set; }
        [ForeignKey("RequestingEmployeeId")]
        public IdentityUser RequestingEmployee { get; set; }

        [Required]
        public string ProductName { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PricePerUnit { get; set; }

        [NotMapped]
        public decimal TotalCost => PricePerUnit * Quantity;

        public ProductRequestStatus Status { get; set; } = ProductRequestStatus.Pending;

        public DateTime DateRequested { get; set; }
    }
}