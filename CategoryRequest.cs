using System.ComponentModel.DataAnnotations;

namespace RaceDay.RequestAndResponseModels
{
    public class CategoryRequest
    {
        [Required]
        [StringLength(80)]
        public string CategoryName { get; set; } = "";

        [StringLength(255)]
        public string? Description { get; set; }
    }
}

