using EduRAG.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace EduRAG.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(IConfiguration config)
        => _basePath = config["Storage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "storage", "materials");

    public async Task<string> SaveAsync(Stream fileStream, string fileName,
                                         int classId, int subjectId, int? chapterId)
    {
        var folder = Path.Combine(_basePath, classId.ToString(), subjectId.ToString(),
                                  chapterId?.ToString() ?? "general");
        Directory.CreateDirectory(folder);

        var storedName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        var fullPath   = Path.Combine(folder, storedName);

        await using var dest = File.Create(fullPath);
        await fileStream.CopyToAsync(dest);

        return Path.GetRelativePath(_basePath, fullPath).Replace('\\', '/');
    }

    public async Task DeleteAsync(string storedPath)
    {
        var fullPath = Path.Combine(_basePath, storedPath);
        if (File.Exists(fullPath))
            await Task.Run(() => File.Delete(fullPath));
    }

    public Stream OpenRead(string storedPath)
        => File.OpenRead(Path.Combine(_basePath, storedPath));
}
