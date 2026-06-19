using EduRAG.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using EduRAG.Infrastructure.Persistence;

namespace EduRAG.Infrastructure.BackgroundJobs;

public class PendingMaterialsRequeueService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Channel<Guid> _queue;
    private readonly ILogger<PendingMaterialsRequeueService> _logger;

    public PendingMaterialsRequeueService(
        IServiceScopeFactory scopeFactory,
        Channel<Guid> queue,
        ILogger<PendingMaterialsRequeueService> logger)
    {
        _scopeFactory = scopeFactory;
        _queue        = queue;
        _logger       = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ids = await db.StudyMaterials
            .Where(m => m.VectorizationStatus == VectorizationStatus.Pending
                     || m.VectorizationStatus == VectorizationStatus.Failed)
            .Select(m => m.Id)
            .ToListAsync(ct);

        foreach (var id in ids)
            await _queue.Writer.WriteAsync(id, ct);

        if (ids.Count > 0)
            _logger.LogInformation("Re-queued {Count} pending/failed material(s) for vectorization.", ids.Count);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
