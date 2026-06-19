using System.ComponentModel.DataAnnotations;

namespace EduRAG.Application.DTOs;

public record CreateSubjectRequest(
    [Required][MaxLength(150)] string Name,
    [MaxLength(1000)] string Description = "");

public record UpdateSubjectRequest(
    [Required][MaxLength(150)] string Name,
    [MaxLength(1000)] string Description,
    bool IsActive);

public record SubjectDto(int Id, string Name, string Description, int ClassId, bool IsActive);
