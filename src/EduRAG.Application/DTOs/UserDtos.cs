using EduRAG.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace EduRAG.Application.DTOs;

public record UserDto(Guid Id, string Email, string FullName, UserRole Role, bool IsActive, DateTime CreatedAt, DateTime? LastLoginAt, int? ClassId);

public record CreateStudentRequest(
    [Required][EmailAddress][MaxLength(255)] string Email,
    [Required][MaxLength(200)] string FullName,
    [Required][MinLength(8)] string Password,
    [Required] int ClassId);

public record UpdateStudentRequest(
    [Required][MaxLength(200)] string FullName,
    [Required][EmailAddress][MaxLength(255)] string Email,
    [Required] int ClassId,
    bool IsActive,
    [MinLength(8)] string? NewPassword);

public record SetStudentPermissionsRequest([Required] IEnumerable<int> SubjectIds);

public record StudentPermissionDto(Guid Id, Guid StudentId, int SubjectId, string SubjectName, DateTime GrantedAt);

public record StudentClassDto(int ClassId, string ClassName, int Grade);

public record StudentSubjectDto(int SubjectId, string SubjectName, string Description);
