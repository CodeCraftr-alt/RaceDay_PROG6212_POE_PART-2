using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaceDay.Models
{
    public class RaceEvent
    {
        [Key]
        public int EventID { get; set; }
        // This is the ID of the Organiser who created the event.
        public int OrganizerID { get; set; }
        public int CategoryID { get; set; }
        public int RouteID { get; set; }
        public string EventName { get; set; } = "";
        public string Description { get; set; } = "";
        public string EventType { get; set; } = "Run";
        public DateOnly EventDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public string Location { get; set; } = "";
        public int Capacity { get; set; }
        public string Status { get; set; } = "Open";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

