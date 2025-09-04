using System;
using System.ComponentModel.DataAnnotations;

namespace IT15.Models
{
    public class DailyLog
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // Links the log to a user

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

       
    }
}

