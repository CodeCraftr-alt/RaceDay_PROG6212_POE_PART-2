using System.ComponentModel.DataAnnotations;

namespace RaceDay.RequestAndResponseModels
{
    public class RegisterRequest
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = "";

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = "";

        [Required]
        [EmailAddress]
        [StringLength(120)]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(128, MinimumLength = 8)]
        public string Password { get; set; } = "";

        [Required]
        [RegularExpression("^(Participant|Organiser)$")]
        public string Role { get; set; } = "Participant";

        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; }
    }
}

