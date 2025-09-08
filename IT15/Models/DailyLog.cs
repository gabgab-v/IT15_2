using System;
using System.ComponentModel.DataAnnotations;

namespace IT15.Models
{
    public class DailyLog
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [Display(Name = "Check-In Time")]
        public DateTime CheckInTime { get; set; }

        [Display(Name = "Check-Out Time")]
        public DateTime? CheckOutTime { get; set; } // Nullable, as it's empty until they check out
    }
}

