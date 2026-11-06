using AgriStream.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriStream.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Device> Devices => Set<Device>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Device>().HasData(
                new Device { Id = 1, DeviceId = "SENSOR-001", Name = "North Field Soil Sensor", Status = "Idle", LastUpdated = new DateTime(2026, 11, 1) },
                new Device { Id = 2, DeviceId = "SENSOR-002", Name = "South Field Soil Sensor", Status = "Idle", LastUpdated = new DateTime(2026, 11, 1) },
                new Device { Id = 3, DeviceId = "VALVE-001", Name = "Main Irrigation Valve", Status = "Idle", LastUpdated = new DateTime(2026, 11, 1) }
            );
        }
    }
}
