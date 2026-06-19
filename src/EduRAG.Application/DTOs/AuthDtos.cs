using EduRAG.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace EduRAG.Application.DTOs;

public record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required] string Password);

public record LoginResponse(string Token, string Role, string FullName, Guid UserId);

public record RegisterRequest(
    [Required][EmailAddress][MaxLength(255)] string Email,
    [Required][MaxLength(200)] string FullName,
    [Required][MinLength(8)] string Password,
    UserRole Role);
