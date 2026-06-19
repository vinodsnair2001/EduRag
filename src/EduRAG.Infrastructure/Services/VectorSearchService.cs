using Dapper;
using EduRAG.Application.DTOs;
using EduRAG.Application.Interfaces;
using System.Data;
using System.Globalization;

namespace EduRAG.Infrastructure.Services;

public class VectorSearchService : IVectorSearchService
{
    private readonly IDbConnection _db;
    public VectorSearchService(IDbConnection db) => _db = db;

    public async Task<IEnumerable<ChunkSearchResult>> SearchAsync(
        float[] queryEmbedding, int classId, int subjectId, int topK = 5)
    {
        // Must use InvariantCulture: a locale with comma decimal separator would
        // render floats as "0,5" and corrupt the vector literal (768 → 1536 tokens).
        var vectorLiteral = "[" +
            string.Join(",", queryEmbedding.Select(f => f.ToString("R", CultureInfo.InvariantCulture))) +
            "]";
        const string sql = @"
            SELECT ""Id"" AS ChunkId, ""Content"", ""PageNumber"",
                   1 - (""Embedding"" <=> @vector::vector(768)) AS Score
            FROM ""MaterialChunks""
            WHERE ""ClassId"" = @classId AND ""SubjectId"" = @subjectId
            ORDER BY ""Embedding"" <=> @vector::vector(768)
            LIMIT @topK";
        return await _db.QueryAsync<ChunkSearchResult>(sql, new { vector = vectorLiteral, classId, subjectId, topK });
    }
}
