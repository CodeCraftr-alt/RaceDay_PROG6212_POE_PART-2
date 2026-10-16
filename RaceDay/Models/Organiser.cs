using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaceDay.Models
{
    public class Organiser
    {
        [Key]
        public int OrganiserID { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        // Never send the password hash back in a JSON response.
        [JsonIgnore]
        public string PasswordHash { get; set; } = "";
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

