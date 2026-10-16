using System.ComponentModel.DataAnnotations;

namespace RaceDay.RequestAndResponseModels
{
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        [StringLength(120)]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(128)]
        public string Password { get; set; } = "";
    }
}

