namespace WebApi.Services;

public class LocalImageStore(IWebHostEnvironment env) : IImageStore
{
    public async Task<string> SaveAsync(Guid userId, IFormFile file)
    {
        var ext      = Path.GetExtension(file.FileName).ToLowerInvariant();
        var webRoot  = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var dir      = Path.Combine(webRoot, "uploads");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, $"{userId}{ext}");

        using var stream = File.Create(filePath);
        await file.CopyToAsync(stream);

        return $"/uploads/{userId}{ext}";
    }
}
