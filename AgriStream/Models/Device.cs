using System.ComponentModel.DataAnnotations;

namespace AgriStream.Models
{
    public class Device
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string DeviceId { get; set; } = "";

        [MaxLength(100)]
        public string Name { get; set; } = "";

        [MaxLength(30)]
        public string Status { get; set; } = "Idle";

        public DateTime LastUpdated { get; set; }
    }
}
