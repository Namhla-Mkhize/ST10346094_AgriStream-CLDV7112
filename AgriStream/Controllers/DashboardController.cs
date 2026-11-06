using System.Security.Claims;
using AgriStream.Data;
using AgriStream.Models.ViewModels;
using AgriStream.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriStream.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly FunctionApiService _api;
    private readonly AppDbContext _db;

    public DashboardController(FunctionApiService api, AppDbContext db)
    {
        _api = api;
        _db = db;
    }

    public async Task<IActionResult> Index(string? deviceId)
    {
        var vm = new DashboardViewModel();

        try
        {
            vm.Readings = await _api.GetTelemetryAsync(deviceId, 10);
        }
        catch (Exception)
        {
            vm.Error = "Couldn't load telemetry from the function.";
        }

        vm.Devices = await _db.Devices.OrderBy(d => d.DeviceId).ToListAsync();
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Farmer,Admin")]
    public async Task<IActionResult> TriggerIrrigation(string deviceId, int minutes = 15)
    {
        if (minutes < 1 || minutes > 60) minutes = 15;

        if (!await _db.Devices.AnyAsync(d => d.DeviceId == deviceId))
        {
            TempData["Error"] = "Unknown device.";
            return RedirectToAction("Index");
        }

        var user = User.FindFirstValue(ClaimTypes.Name) ?? "unknown";
        var ok = await _api.TriggerIrrigationAsync(deviceId, minutes, user);

        if (ok)
            TempData["Success"] = $"Irrigation command queued for {deviceId}.";
        else
            TempData["Error"] = "The function rejected the command.";

        return RedirectToAction("Index");
    }
}