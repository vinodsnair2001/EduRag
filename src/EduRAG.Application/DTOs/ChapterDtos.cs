using System.ComponentModel.DataAnnotations;

namespace EduRAG.Application.DTOs;

public record CreateChapterRequest(
    [Required][MaxLength(200)] string Title,
    int OrderIndex = 0);

public record UpdateChapterRequest(
    [Required][MaxLength(200)] string Title,
    int OrderIndex,
    bool IsActive);

public record ChapterDto(int Id, string Title, int OrderIndex, int SubjectId, bool IsActive, bool HasPdf);
