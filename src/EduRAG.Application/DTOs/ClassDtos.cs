using System.ComponentModel.DataAnnotations;

namespace EduRAG.Application.DTOs;

public record CreateClassRequest(
    [Required][MaxLength(100)] string Name,
    [Range(1, 12)] int Grade);

public record UpdateClassRequest(
    [Required][MaxLength(100)] string Name,
    [Range(1, 12)] int Grade,
    bool IsActive);

public record ClassDto(int Id, string Name, int Grade, bool IsActive, DateTime CreatedAt);

public record ClassWithSubjectsDto(int Id, string Name, int Grade, List<SubjectDto> Subjects);
