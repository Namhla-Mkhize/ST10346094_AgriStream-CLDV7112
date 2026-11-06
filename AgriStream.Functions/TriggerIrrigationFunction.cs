using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AgriStream.Functions;

public class IrrigationCommand
{
    public string DeviceId { get; set; } = "";
    public string Command { get; set; } = "StartIrrigation";
    public int DurationMinutes { get; set; } = 15;
    public string RequestedBy { get; set; } = "system";
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}

public class IrrigationOutput
{
    [QueueOutput("irrigation-commands", Connection = "AzureWebJobsStorage")]
    public string? QueueMessage { get; set; }

    [HttpResult]
    public HttpResponseData HttpResponse { get; set; } = default!;
}

public class TriggerIrrigationFunction
{
    [Function("TriggerIrrigation")]
    public async Task<IrrigationOutput> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "irrigation")] HttpRequestData req)
    {
        IrrigationCommand? cmd = null;
        try
        {
            cmd = await req.ReadFromJsonAsync<IrrigationCommand>();
        }
        catch (JsonException) { }

        if (cmd == null || string.IsNullOrWhiteSpace(cmd.DeviceId))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Body must be JSON with a deviceId.");
            return new IrrigationOutput { HttpResponse = bad };
        }

        cmd.RequestedAt = DateTime.UtcNow;

        var ok = req.CreateResponse(HttpStatusCode.Accepted);
        await ok.WriteAsJsonAsync(new { message = "Irrigation command queued", cmd.DeviceId });

        return new IrrigationOutput
        {
            QueueMessage = JsonSerializer.Serialize(cmd),
            HttpResponse = ok
        };
    }
}