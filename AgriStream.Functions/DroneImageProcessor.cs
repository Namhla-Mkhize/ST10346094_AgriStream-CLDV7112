using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions;

public class DroneImageProcessor
{
    private readonly ILogger<DroneImageProcessor> _logger;

    public DroneImageProcessor(ILogger<DroneImageProcessor> logger)
    {
        _logger = logger;
    }

    [Function("DroneImageBlobTrigger")]
    public async Task Run(
        [BlobTrigger("drone-images/{name}", Connection = "AzureWebJobsStorage")] Stream blob,
        string name)
    {
        _logger.LogInformation("Drone image uploaded: {Name}, {Size} bytes", name, blob.Length);

        var connection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        var table = new TableClient(connection, "ProcessingAudit");
        await table.CreateIfNotExistsAsync();

        var entity = new TableEntity("DroneImage", $"{DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks:D19}")
        {
            { "BlobName", name },
            { "SizeBytes", blob.Length },
            { "Status", "Processed" },
            { "ProcessedAt", DateTime.UtcNow }
        };

        await table.AddEntityAsync(entity);

        _logger.LogInformation("Audit entry written for {Name}", name);
    }
}