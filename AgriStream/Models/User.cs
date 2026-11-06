using System.ComponentModel.DataAnnotations;

namespace AgriStream.Models
{
    public class User
    { 
        public int Id { get; set; } 

        [Required, MaxLength(50)]
        public string Username { get; set; } = "";

        [Required]
        public string PasswordHash { get; set; } = "";

        [Required, MaxLength(20)]
        public string Role { get; set; } = "Farmer"; // Farmer, Agronomist, Admin
    }
}
