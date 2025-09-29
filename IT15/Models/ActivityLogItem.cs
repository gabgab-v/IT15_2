using System;

namespace IT15.Models
{
    // A simple class to represent an item in the user's activity feed.
    public class ActivityLogItem
    {
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; } // The name of the Lucide icon to display
        public string Url { get; set; } // A link to the relevant page
    }
}
