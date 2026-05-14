using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace WebApi.Services;

public class BlobImageStore(IConfiguration config) : IImageStore
{
    private const string ContainerName = "profile-pictures";

    public async Task<string> SaveAsync(Guid userId, IFormFile file)
    {
        var connStr   = config["BlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("BlobStorage:ConnectionString is not configured.");
        var container = new BlobContainerClient(connStr, ContainerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var ext      = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blobName = $"{userId}{ext}";
        var blob     = container.GetBlobClient(blobName);

        using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, overwrite: true);

        return blob.Uri.ToString();
    }
}
