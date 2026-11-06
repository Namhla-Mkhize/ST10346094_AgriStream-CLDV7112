using AgriStream.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriStream.Controllers;

[Authorize]
public class TelemetryController : Controller
{
    private readonly TelemetryService _telemetry;

    public TelemetryController(TelemetryService telemetry)
    {
        _telemetry = telemetry;
    }

    public IActionResult Index() => View();

    [HttpPost]
    public async Task<IActionResult> Simulate(int count = 20)
    {
        if (count < 1 || count > 100) count = 20;

        await _telemetry.AddManyAsync(count);
        TempData["Success"] = $"{count} readings written to Table Storage.";
        return RedirectToAction("Index");
    }
}