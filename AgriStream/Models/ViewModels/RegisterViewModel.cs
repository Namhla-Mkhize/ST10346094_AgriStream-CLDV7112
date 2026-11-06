using System.ComponentModel.DataAnnotations;

namespace AgriStream.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required, MaxLength(50)]
        public string Username { get; set; } = "";

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required]
        public string Role { get; set; } = "Farmer";
    }
}
