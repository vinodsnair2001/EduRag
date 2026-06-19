using EduRAG.Domain.Enums;

namespace EduRAG.Application.DTOs;

public record UploadMaterialResponse(Guid MaterialId, VectorizationStatus Status);

public record MaterialDto(
    Guid Id,
    string OriginalFileName,
    long FileSizeBytes,
    VectorizationStatus Status,
    DateTime UploadedAt,
    string? VectorizationError,
    int ClassId,
    int SubjectId,
    int? ChapterId);
