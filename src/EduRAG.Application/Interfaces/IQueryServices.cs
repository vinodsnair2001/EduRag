using EduRAG.Application.DTOs;

namespace EduRAG.Application.Interfaces;

public interface IChatQueries
{
    Task<IEnumerable<ChatMessageDto>> GetLastNMessagesAsync(Guid sessionId, int n);
    Task<IEnumerable<ChatMessageDto>> GetSessionMessagesAsync(Guid sessionId);
}

public interface ISubjectQueries
{
    Task<SubjectDto?> GetByIdAsync(int subjectId);
    Task<IEnumerable<SubjectDto>> GetByClassIdAsync(int classId);
    Task<IEnumerable<SubjectDto>> GetByClassIdForStudentAsync(int classId, Guid studentId);
}

public interface IClassQueries
{
    Task<IEnumerable<ClassDto>> GetAllActiveAsync();
    Task<ClassDto?>             GetByIdAsync(int classId);
}

public interface IChapterQueries
{
    Task<IEnumerable<ChapterDto>> GetBySubjectIdAsync(int subjectId);
}

public interface IMaterialQueries
{
    Task<IEnumerable<MaterialDto>> GetAllAsync();
    Task<IEnumerable<MaterialDto>> GetBySubjectAsync(int subjectId);
    Task<MaterialFileDto?>          GetFileByChapterIdAsync(int chapterId);
}

public interface IUserQueries
{
    Task<IEnumerable<UserDto>> GetAllAsync();
}

public interface IStudentPermissionQueries
{
    Task<IEnumerable<StudentPermissionDto>> GetByStudentIdAsync(Guid studentId);
    Task<StudentClassDto?>                  GetStudentClassAsync(Guid studentId);
    Task<IEnumerable<StudentSubjectDto>>    GetPermittedSubjectsAsync(Guid studentId);
}
