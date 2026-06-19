using EduRAG.Application.Interfaces;
using EduRAG.Domain.Entities;

namespace EduRAG.Infrastructure.Persistence.Repositories;

public class ChapterRepository : IChapterRepository
{
    private readonly AppDbContext _ctx;
    public ChapterRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<Chapter> CreateAsync(Chapter entity)
    {
        _ctx.Chapters.Add(entity);
        await _ctx.SaveChangesAsync();
        return entity;
    }

    public async Task<Chapter> UpdateAsync(Chapter entity)
    {
        var existing = await _ctx.Chapters.FindAsync(entity.Id)
            ?? throw new KeyNotFoundException($"Chapter {entity.Id} not found.");
        existing.Title      = entity.Title;
        existing.OrderIndex = entity.OrderIndex;
        existing.IsActive   = entity.IsActive;
        await _ctx.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _ctx.Chapters.FindAsync(id)
            ?? throw new KeyNotFoundException($"Chapter {id} not found.");
        _ctx.Chapters.Remove(entity);
        await _ctx.SaveChangesAsync();
    }
}
