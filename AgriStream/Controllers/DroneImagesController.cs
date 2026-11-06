using AgriStream.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriStream.Controllers;

[Authorize]
public class DroneImagesController : Controller
{
    private readonly BlobService _blobService;

    public DroneImagesController(BlobService blobService)
    {
        _blobService = blobService;
    }

    public async Task<IActionResult> Index()
    {
        var blobs = await _blobService.ListAsync();
        return View(blobs);
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Pick a file first.";
            return RedirectToAction("Index");
        }

        if (!file.ContentType.StartsWith("image/"))
        {
            TempData["Error"] = "Only image files are allowed.";
            return RedirectToAction("Index");
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            TempData["Error"] = "Max file size is 10 MB.";
            return RedirectToAction("Index");
        }

        var name = await _blobService.UploadAsync(file);
        TempData["Success"] = $"Uploaded {name}";
        return RedirectToAction("Index");
    }
}