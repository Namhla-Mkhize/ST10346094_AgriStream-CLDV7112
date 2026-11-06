using System.Net.Http.Json;
using System.Text.Json;
using AgriStream.Models;

namespace AgriStream.Services;

public class FunctionApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public FunctionApiService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _http.BaseAddress = new Uri(config["FunctionApi:BaseUrl"]!);

        var key = config["FunctionApi:Key"];
        if (!string.IsNullOrWhiteSpace(key))
            _http.DefaultRequestHeaders.Add("x-functions-key", key);
    }

    public async Task<List<TelemetryReading>> GetTelemetryAsync(string? deviceId = null, int top = 10)
    {
        var url = $"api/telemetry?top={top}";
        if (!string.IsNullOrWhiteSpace(deviceId))
            url += $"&deviceId={Uri.EscapeDataString(deviceId)}";

        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<TelemetryReading>>(json, _json) ?? new();
    }

    public async Task<bool> TriggerIrrigationAsync(string deviceId, int minutes, string requestedBy)
    {
        var body = new
        {
            deviceId,
            command = "StartIrrigation",
            durationMinutes = minutes,
            requestedBy
        };

        var response = await _http.PostAsJsonAsync("api/irrigation", body);
        return response.IsSuccessStatusCode;
    }
}