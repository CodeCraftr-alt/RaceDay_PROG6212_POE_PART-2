using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaceDay.Models
{
    public class RaceResult
    {
        [Key]
        public int ResultID { get; set; }
        public int EnrollmentID { get; set; }
        public TimeOnly FinishTime { get; set; }
        public int Position { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}

