using System;
using System.ComponentModel.DataAnnotations;

namespace IT15.Models // <-- Make sure this namespace matches your project name
{
    public class DailyLog
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // To link the log to a user

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [Display(Name = "Log Entry")]
        public string LogContent { get; set; }
    }
}
