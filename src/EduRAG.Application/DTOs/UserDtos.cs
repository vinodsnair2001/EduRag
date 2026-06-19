using EduRAG.Domain.Enums;

namespace EduRAG.Application.DTOs;

public record UserDto(Guid Id, string Email, string FullName, UserRole Role, bool IsActive, DateTime CreatedAt, DateTime? LastLoginAt);
