using System.ComponentModel.DataAnnotations;

namespace RaceDay.RequestAndResponseModels
{
    public class RouteRequest
    {
        [Required]
        [StringLength(100)]
        public string RouteName { get; set; } = "";

        [Required]
        [StringLength(150)]
        public string StartLocation { get; set; } = "";

        [Required]
        [StringLength(150)]
        public string FinishLocation { get; set; } = "";

        [Range(typeof(decimal), "0.01", "9999.99")]
        public decimal DistanceKm { get; set; }

        [Range(0, int.MaxValue)]
        public int ElevationGainM { get; set; }
    }
}

