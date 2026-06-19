using EduRAG.Application.Interfaces;
using EduRAG.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduRAG.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _ctx;
    public UserRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<AppUser> CreateAsync(AppUser user)
    {
        _ctx.AppUsers.Add(user);
        await _ctx.SaveChangesAsync();
        return user;
    }

    public async Task<AppUser?> GetByEmailAsync(string email)
        => await _ctx.AppUsers.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());

    public async Task<AppUser?> GetByIdAsync(Guid id)
        => await _ctx.AppUsers.FindAsync(id);

    public async Task UpdateLastLoginAsync(Guid id)
    {
        var user = await _ctx.AppUsers.FindAsync(id);
        if (user is not null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _ctx.SaveChangesAsync();
        }
    }
}
