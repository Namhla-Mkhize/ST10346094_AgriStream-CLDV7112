using System.Net;
using System.Text.RegularExpressions;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AgriStream.Functions;

public class GetTelemetryFunction
{
    private static readonly string[] DefaultDevices = { "SENSOR-001", "SENSOR-002" };

    [Function("GetLatestTelemetry")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "telemetry")] HttpRequestData req)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var deviceId = query["deviceId"];
        if (!int.TryParse(query["top"], out var top) || top < 1 || top > 50) top = 10;

        var devices = DefaultDevices;
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            if (!Regex.IsMatch(deviceId, "^[A-Za-z0-9-]{1,50}$"))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Invalid deviceId.");
                return bad;
            }
            devices = new[] { deviceId };
        }

        var connection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        var table = new TableClient(connection, "Telemetry");
        await table.CreateIfNotExistsAsync();

        var results = new List<object>();

        foreach (var device in devices)
        {
            var count = 0;
            await foreach (var e in table.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{device}'"))
            {
                results.Add(new
                {
                    DeviceId = e.PartitionKey,
                    ReadingTime = e.Timestamp,
                    SoilMoisture = e.GetDouble("SoilMoisture"),
                    SoilTemperature = e.GetDouble("SoilTemperature"),
                    Ph = e.GetDouble("Ph")
                });

                if (++count >= top) break;
            }
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(results);
        return response;
    }
}