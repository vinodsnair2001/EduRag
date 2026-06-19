using EduRAG.Application.Interfaces;
using EduRAG.Domain.Entities;
using EduRAG.Domain.Enums;
using EduRAG.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace EduRAG.Infrastructure.BackgroundJobs;

public class VectorizationProcessor
{
    private readonly IStudyMaterialRepository _materialRepo;
    private readonly IMaterialChunkRepository _chunkRepo;
    private readonly IAIService               _ai;
    private readonly IFileStorageService      _storage;
    private readonly PdfProcessingService     _pdfService;
    private readonly ILogger<VectorizationProcessor> _logger;

    public VectorizationProcessor(
        IStudyMaterialRepository materialRepo,
        IMaterialChunkRepository chunkRepo,
        IAIService ai,
        IFileStorageService storage,
        PdfProcessingService pdfService,
        ILogger<VectorizationProcessor> logger)
    {
        _materialRepo = materialRepo;
        _chunkRepo    = chunkRepo;
        _ai           = ai;
        _storage      = storage;
        _pdfService   = pdfService;
        _logger       = logger;
    }

    public async Task ProcessAsync(Guid materialId, CancellationToken ct)
    {
        try
        {
            var material = await _materialRepo.GetByIdAsync(materialId);
            if (material is null)
            {
                _logger.LogWarning("Material {Id} not found, skipping.", materialId);
                return;
            }

            await _materialRepo.UpdateStatusAsync(materialId, VectorizationStatus.Processing);
            _logger.LogInformation("Vectorizing material {Id} ({File})", materialId, material.OriginalFileName);

            using var pdfStream = _storage.OpenRead(material.StoredFilePath);
            var chunks = _pdfService.ExtractAndChunk(pdfStream);

            var entities = new List<MaterialChunk>();
            for (int i = 0; i < chunks.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var (text, page) = chunks[i];
                var embedding = await _ai.GetEmbeddingAsync(text, ct);
                entities.Add(new MaterialChunk
                {
                    MaterialId = materialId,
                    ClassId    = material.ClassId,
                    SubjectId  = material.SubjectId,
                    ChapterId  = material.ChapterId,
                    Content    = text,
                    ChunkIndex = i,
                    PageNumber = page,
                    Embedding  = embedding,
                });
            }

            await _chunkRepo.BulkInsertAsync(entities);
            await _materialRepo.UpdateStatusAsync(materialId, VectorizationStatus.Completed);
            _logger.LogInformation("Vectorized {Count} chunks for material {Id}", entities.Count, materialId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Vectorization cancelled for {Id}", materialId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vectorization failed for {Id}", materialId);
            await _materialRepo.UpdateStatusAsync(materialId, VectorizationStatus.Failed, ex.Message);
        }
    }
}
