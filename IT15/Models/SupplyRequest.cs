using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15.Models
{
    public enum SupplyRequestStatus { Pending, Approved, Denied }

    public class SupplyRequest
    {
        public int Id { get; set; }
        public int SupplyId { get; set; }
        [ForeignKey("SupplyId")]
        public Supply Supply { get; set; }
        public int Quantity { get; set; }
        public string RequestingEmployeeId { get; set; }
        [ForeignKey("RequestingEmployeeId")]
        public IdentityUser RequestingEmployee { get; set; }
        public DateTime DateRequested { get; set; }
        public SupplyRequestStatus Status { get; set; } = SupplyRequestStatus.Pending;
    }
}