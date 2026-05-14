namespace WebApi.Services;

public interface IImageStore
{
    Task<string> SaveAsync(Guid userId, IFormFile file);
}
