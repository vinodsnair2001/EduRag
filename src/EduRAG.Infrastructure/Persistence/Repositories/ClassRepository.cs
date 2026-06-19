using EduRAG.Application.Interfaces;
using EduRAG.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduRAG.Infrastructure.Persistence.Repositories;

public class ClassRepository : IClassRepository
{
    private readonly AppDbContext _ctx;
    public ClassRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<Class> CreateAsync(Class entity)
    {
        _ctx.Classes.Add(entity);
        await _ctx.SaveChangesAsync();
        return entity;
    }

    public async Task<Class> UpdateAsync(Class entity)
    {
        var existing = await _ctx.Classes.FindAsync(entity.Id)
            ?? throw new KeyNotFoundException($"Class {entity.Id} not found.");
        existing.Name     = entity.Name;
        existing.Grade    = entity.Grade;
        existing.IsActive = entity.IsActive;
        await _ctx.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _ctx.Classes.FindAsync(id)
            ?? throw new KeyNotFoundException($"Class {id} not found.");
        _ctx.Classes.Remove(entity);
        await _ctx.SaveChangesAsync();
    }
}
