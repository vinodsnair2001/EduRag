using EduRAG.Application.DTOs;

namespace EduRAG.Application.Interfaces;

public interface IVectorSearchService
{
    /// <summary>
    /// chapterIds: when non-empty, only chunks whose ChapterId is in the list are returned.
    /// Empty or null = no chapter filter (all chunks for the class+subject).
    /// </summary>
    Task<IEnumerable<ChunkSearchResult>> SearchAsync(
        float[] queryEmbedding, int classId, int subjectId,
        int[]? chapterIds = null, int topK = 5);
}
