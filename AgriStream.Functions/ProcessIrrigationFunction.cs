using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions;

public class ProcessIrrigationFunction
{
    private readonly ILogger<ProcessIrrigationFunction> _logger;

    public ProcessIrrigationFunction(ILogger<ProcessIrrigationFunction> logger)
    {
        _logger = logger;
    }

    [Function("ProcessIrrigationCommand")]
    public async Task Run(
        [QueueTrigger("irrigation-commands", Connection = "AzureWebJobsStorage")] string message)
    {
        _logger.LogInformation("Irrigation command received: {Message}", message);

        var cmd = JsonSerializer.Deserialize<IrrigationCommand>(message,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (cmd == null || string.IsNullOrWhiteSpace(cmd.DeviceId))
        {
            _logger.LogWarning("Invalid command, skipping.");
            return;
        }

        var status = cmd.Command == "StopIrrigation" ? "Idle" : "Irrigating";

        var connString = Environment.GetEnvironmentVariable("SqlConnectionString");

        using var conn = new SqlConnection(connString);
        await conn.OpenAsync();

        using var sql = new SqlCommand(
            "UPDATE Devices SET Status = @status, LastUpdated = @now WHERE DeviceId = @deviceId", conn);
        sql.Parameters.AddWithValue("@status", status);
        sql.Parameters.AddWithValue("@now", DateTime.UtcNow);
        sql.Parameters.AddWithValue("@deviceId", cmd.DeviceId);

        var rows = await sql.ExecuteNonQueryAsync();

        if (rows == 0)
            _logger.LogWarning("No device found with DeviceId {DeviceId}", cmd.DeviceId);
        else
            _logger.LogInformation("Device {DeviceId} set to {Status}", cmd.DeviceId, status);
    }
}