using System.ComponentModel.DataAnnotations;

namespace RaceDay.RequestAndResponseModels
{
    public class EventRequest
    {
        [Required]
        [StringLength(120)]
        public string EventName { get; set; } = "";

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = "";

        [Required]
        [RegularExpression("^(Run|Walk|Cycle)$")]
        public string EventType { get; set; } = "Run";

        public DateOnly EventDate { get; set; }
        public TimeOnly StartTime { get; set; }

        [Required]
        [StringLength(150)]
        public string Location { get; set; } = "";

        [Range(1, int.MaxValue)]
        public int Capacity { get; set; }

        [Range(1, int.MaxValue)]
        public int CategoryId { get; set; }

        [Range(1, int.MaxValue)]
        public int RouteId { get; set; }

        // The default category above is always included.
        [Required]
        public List<int> AdditionalCategoryIds { get; set; } = new List<int>();

        [Required]
        [RegularExpression("^(Open|Closed|Cancelled)$")]
        public string Status { get; set; } = "Open";
    }
}

