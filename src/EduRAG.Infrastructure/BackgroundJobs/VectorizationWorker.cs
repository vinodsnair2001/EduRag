using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace EduRAG.Infrastructure.BackgroundJobs;

public class VectorizationWorker : BackgroundService
{
    private readonly Channel<Guid>         _queue;
    private readonly IServiceScopeFactory  _scopeFactory;
    private readonly ILogger<VectorizationWorker> _logger;

    public VectorizationWorker(
        Channel<Guid> queue,
        IServiceScopeFactory scopeFactory,
        ILogger<VectorizationWorker> logger)
    {
        _queue        = queue;
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("VectorizationWorker started.");
        await foreach (var materialId in _queue.Reader.ReadAllAsync(ct))
        {
            _logger.LogInformation("Dequeued material {Id} for vectorization.", materialId);
            using var scope     = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<VectorizationProcessor>();
            await processor.ProcessAsync(materialId, ct);
        }
    }
}
