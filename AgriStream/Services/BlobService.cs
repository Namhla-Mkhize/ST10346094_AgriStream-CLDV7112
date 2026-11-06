using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AgriStream.Services;

public class BlobService
{
    private readonly BlobContainerClient _container;

    public BlobService(IConfiguration config)
    {
        var client = new BlobServiceClient(config["AzureStorage:ConnectionString"]);
        _container = client.GetBlobContainerClient(config["AzureStorage:BlobContainer"]);
        _container.CreateIfNotExists(PublicAccessType.None);
    }

    public async Task<string> UploadAsync(IFormFile file)
    {
        var blobName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var blob = _container.GetBlobClient(blobName);

        using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

        return blobName;
    }

    public async Task<List<BlobItem>> ListAsync()
    {
        var items = new List<BlobItem>();
        await foreach (var item in _container.GetBlobsAsync())
        {
            items.Add(item);
        }
        return items.OrderByDescending(i => i.Properties.CreatedOn).ToList();
    }
}