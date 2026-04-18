namespace StudentInnovation.WebApi.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;

    public LocalFileStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var folder = Path.Combine(_env.ContentRootPath, "uploads", DateTime.UtcNow.ToString("yyyyMMdd"));
        Directory.CreateDirectory(folder);

        var newName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var fullPath = Path.Combine(folder, newName);
        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream, cancellationToken);
        return $"/files/{DateTime.UtcNow:yyyyMMdd}/{newName}";
    }
}
