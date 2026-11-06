namespace AgriStream.Models;

public class TelemetryReading
{
    public string DeviceId { get; set; } = "";
    public DateTimeOffset? ReadingTime { get; set; }
    public double SoilMoisture { get; set; }
    public double SoilTemperature { get; set; }
    public double Ph { get; set; }
}