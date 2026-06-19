using EduRAG.Application.Interfaces;
using EduRAG.Domain.Entities;
using EduRAG.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EduRAG.Infrastructure.Persistence.Repositories;

public class StudyMaterialRepository : IStudyMaterialRepository
{
    private readonly AppDbContext _ctx;
    public StudyMaterialRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<StudyMaterial> CreateAsync(StudyMaterial entity)
    {
        _ctx.StudyMaterials.Add(entity);
        await _ctx.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateStatusAsync(Guid id, VectorizationStatus status, string? error = null)
    {
        var entity = await _ctx.StudyMaterials.FindAsync(id)
            ?? throw new KeyNotFoundException($"StudyMaterial {id} not found.");
        entity.VectorizationStatus = status;
        entity.VectorizationError  = error;
        await _ctx.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _ctx.StudyMaterials.FindAsync(id)
            ?? throw new KeyNotFoundException($"StudyMaterial {id} not found.");
        _ctx.StudyMaterials.Remove(entity);
        await _ctx.SaveChangesAsync();
    }

    public async Task<StudyMaterial?> GetByHashAsync(string contentHash)
        => await _ctx.StudyMaterials.FirstOrDefaultAsync(m => m.ContentHash == contentHash);

    public async Task<StudyMaterial?> GetByIdAsync(Guid id)
        => await _ctx.StudyMaterials.FindAsync(id);
}
