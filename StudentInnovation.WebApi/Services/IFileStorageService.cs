namespace StudentInnovation.WebApi.Services;

public interface IFileStorageService
{
    Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);
}
