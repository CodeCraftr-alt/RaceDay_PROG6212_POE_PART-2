using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaceDay.Models
{
    public class EventCategory
    {
        [Key]
        public int EventCategoryID { get; set; }
        // One row means this event offers this category.
        public int EventID { get; set; }
        public int CategoryID { get; set; }
    }
}

