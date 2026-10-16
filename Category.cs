using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaceDay.Models
{
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = "";
        public string? Description { get; set; }
    }
}

