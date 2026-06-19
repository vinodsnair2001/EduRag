using EduRAG.Application.DTOs;

namespace EduRAG.Application.Interfaces;

public interface IVectorSearchService
{
    Task<IEnumerable<ChunkSearchResult>> SearchAsync(
        float[] queryEmbedding, int classId, int subjectId, int topK = 5);
}
