using Dapper;
using EduRAG.Application.DTOs;
using EduRAG.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Globalization;

namespace EduRAG.Infrastructure.Services;

public class VectorSearchService : IVectorSearchService
{
    private readonly IDbConnection _db;
    private readonly int           _dimensions;

    public VectorSearchService(IDbConnection db, IConfiguration config)
    {
        _db         = db;
        _dimensions = config.GetValue<int>("AI:EmbeddingDimensions", 768);
    }

    public async Task<IEnumerable<ChunkSearchResult>> SearchAsync(
        float[] queryEmbedding, int classId, int subjectId,
        int[]? chapterIds = null, int topK = 5)
    {
        // Must use InvariantCulture: a locale with comma decimal separator would
        // render floats as "0,5" and corrupt the vector literal (768 → 1536 tokens).
        var vectorLiteral = "[" +
            string.Join(",", queryEmbedding.Select(f => f.ToString("R", CultureInfo.InvariantCulture))) +
            "]";

        var hasChapterFilter = chapterIds is { Length: > 0 };

        // _dimensions comes from trusted config (integer), not user input — safe to interpolate.
        // Chapter filter appended when the student selected specific chapters.
        var sql = $@"
            SELECT ""Id"" AS ChunkId, ""Content"", ""PageNumber"",
                   1 - (""Embedding"" <=> @vector::vector({_dimensions})) AS Score
            FROM ""MaterialChunks""
            WHERE ""ClassId"" = @classId AND ""SubjectId"" = @subjectId
              {(hasChapterFilter ? @"AND ""ChapterId"" = ANY(@chapterIds)" : "")}
            ORDER BY ""Embedding"" <=> @vector::vector({_dimensions})
            LIMIT @topK";

        return await _db.QueryAsync<ChunkSearchResult>(sql,
            new { vector = vectorLiteral, classId, subjectId, chapterIds, topK });
    }
}
