using EduRAG.Application.Interfaces;
using EduRAG.Domain.Entities;

namespace EduRAG.Infrastructure.Persistence.Repositories;

public class SubjectRepository : ISubjectRepository
{
    private readonly AppDbContext _ctx;
    public SubjectRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<Subject> CreateAsync(Subject entity)
    {
        _ctx.Subjects.Add(entity);
        await _ctx.SaveChangesAsync();
        return entity;
    }

    public async Task<Subject> UpdateAsync(Subject entity)
    {
        var existing = await _ctx.Subjects.FindAsync(entity.Id)
            ?? throw new KeyNotFoundException($"Subject {entity.Id} not found.");
        existing.Name        = entity.Name;
        existing.Description = entity.Description;
        existing.IsActive    = entity.IsActive;
        await _ctx.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _ctx.Subjects.FindAsync(id)
            ?? throw new KeyNotFoundException($"Subject {id} not found.");
        _ctx.Subjects.Remove(entity);
        await _ctx.SaveChangesAsync();
    }
}
