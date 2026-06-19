using EduRAG.Application.Interfaces;
using EduRAG.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduRAG.Infrastructure.Persistence.Repositories;

public class MaterialChunkRepository : IMaterialChunkRepository
{
    private readonly AppDbContext _ctx;
    public MaterialChunkRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task BulkInsertAsync(IEnumerable<MaterialChunk> chunks)
    {
        _ctx.MaterialChunks.AddRange(chunks);
        await _ctx.SaveChangesAsync();
    }

    public async Task DeleteByMaterialAsync(Guid materialId)
    {
        var chunks = await _ctx.MaterialChunks
            .Where(c => c.MaterialId == materialId)
            .ToListAsync();
        _ctx.MaterialChunks.RemoveRange(chunks);
        await _ctx.SaveChangesAsync();
    }
}
