namespace AgriStream.Models.ViewModels;

public class DashboardViewModel
{
    public List<TelemetryReading> Readings { get; set; } = new();
    public List<Device> Devices { get; set; } = new();
    public string? Error { get; set; }
}