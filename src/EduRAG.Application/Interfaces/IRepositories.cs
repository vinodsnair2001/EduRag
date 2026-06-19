using EduRAG.Domain.Entities;
using EduRAG.Domain.Enums;

namespace EduRAG.Application.Interfaces;

public interface IClassRepository
{
    Task<Class>   CreateAsync(Class entity);
    Task<Class>   UpdateAsync(Class entity);
    Task          DeleteAsync(int id);
}

public interface ISubjectRepository
{
    Task<Subject> CreateAsync(Subject entity);
    Task<Subject> UpdateAsync(Subject entity);
    Task          DeleteAsync(int id);
}

public interface IChapterRepository
{
    Task<Chapter> CreateAsync(Chapter entity);
    Task<Chapter> UpdateAsync(Chapter entity);
    Task          DeleteAsync(int id);
}

public interface IStudyMaterialRepository
{
    Task<StudyMaterial>  CreateAsync(StudyMaterial entity);
    Task                 UpdateStatusAsync(Guid id, VectorizationStatus status, string? error = null);
    Task                 DeleteAsync(Guid id);
    Task<StudyMaterial?> GetByHashAsync(string contentHash);
    Task<StudyMaterial?> GetByIdAsync(Guid id);
}

public interface IMaterialChunkRepository
{
    Task BulkInsertAsync(IEnumerable<MaterialChunk> chunks);
    Task DeleteByMaterialAsync(Guid materialId);
}

public interface IChatRepository
{
    Task<ChatSession>  CreateSessionAsync(ChatSession session);
    Task<ChatSession?> GetSessionAsync(Guid sessionId);
    Task<ChatMessage>  AddMessageAsync(ChatMessage message);
}

public interface IUserRepository
{
    Task<AppUser>  CreateAsync(AppUser user);
    Task<AppUser?> GetByEmailAsync(string email);
    Task<AppUser?> GetByIdAsync(Guid id);
    Task           UpdateLastLoginAsync(Guid id);
}
