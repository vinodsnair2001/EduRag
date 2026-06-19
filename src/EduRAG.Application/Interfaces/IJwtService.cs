using EduRAG.Domain.Entities;

namespace EduRAG.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(AppUser user);
}
