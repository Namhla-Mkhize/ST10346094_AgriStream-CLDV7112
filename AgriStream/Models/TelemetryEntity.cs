using Azure;
using Azure.Data.Tables;

namespace AgriStream.Models;

public class TelemetryEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "";   // DeviceId
    public string RowKey { get; set; } = "";         // reversed ticks
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public double SoilMoisture { get; set; }   // %
    public double SoilTemperature { get; set; } // °C
    public double Ph { get; set; }
    public string EventType { get; set; } = "SensorReading"; // SensorReading, IrrigationEvent
}