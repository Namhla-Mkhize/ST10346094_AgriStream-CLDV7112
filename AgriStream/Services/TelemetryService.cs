using Azure.Data.Tables;
using AgriStream.Models;

namespace AgriStream.Services;

public class TelemetryService
{
    private readonly TableClient _table;
    private static readonly Random _rng = new();

    public TelemetryService(IConfiguration config)
    {
        var client = new TableServiceClient(config["AzureStorage:ConnectionString"]);
        _table = client.GetTableClient("Telemetry");
        _table.CreateIfNotExists();
    }

    public async Task AddReadingAsync(string deviceId)
    {
        var entity = new TelemetryEntity
        {
            PartitionKey = deviceId,
            RowKey = $"{DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks:D19}_{Guid.NewGuid().ToString()[..6]}",
            SoilMoisture = Math.Round(15 + _rng.NextDouble() * 55, 1),
            SoilTemperature = Math.Round(12 + _rng.NextDouble() * 20, 1),
            Ph = Math.Round(5.5 + _rng.NextDouble() * 2.5, 1)
        };

        await _table.AddEntityAsync(entity);
    }

    public async Task AddManyAsync(int count)
    {
        var devices = new[] { "SENSOR-001", "SENSOR-002" };
        for (int i = 0; i < count; i++)
        {
            await AddReadingAsync(devices[i % devices.Length]);
        }
    }
}