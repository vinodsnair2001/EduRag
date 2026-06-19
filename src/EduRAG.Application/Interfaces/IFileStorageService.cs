namespace EduRAG.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream fileStream, string fileName,
                           int classId, int subjectId, int? chapterId);
    Task DeleteAsync(string storedPath);
    Stream OpenRead(string storedPath);
}
