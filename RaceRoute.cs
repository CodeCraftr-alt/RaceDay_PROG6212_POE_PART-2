using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaceDay.Models
{
    public class RaceRoute
    {
        [Key]
        public int RouteID { get; set; }
        public string RouteName { get; set; } = "";
        public string StartLocation { get; set; } = "";
        public string FinishLocation { get; set; } = "";
        public decimal DistanceKm { get; set; }
        public int ElevationGainM { get; set; }
    }
}

