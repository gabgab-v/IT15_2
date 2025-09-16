using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15.Models
{
    public class DailyLog
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public IdentityUser User { get; set; }

        [Required]
        [Display(Name = "Check-In Time")]
        public DateTime CheckInTime { get; set; }

        [Display(Name = "Check-Out Time")]
        public DateTime? CheckOutTime { get; set; }

        public AttendanceStatus Status { get; set; }

        // --- NEW PROPERTIES FOR OVERTIME ---
        [Column(TypeName = "decimal(5, 2)")]
        public decimal? OvertimeHours { get; set; }

        public OvertimeStatus OvertimeStatus { get; set; } = OvertimeStatus.NotApplicable;
    }
}