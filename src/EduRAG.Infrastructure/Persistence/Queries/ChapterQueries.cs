using Dapper;
using EduRAG.Application.DTOs;
using EduRAG.Application.Interfaces;
using System.Data;

namespace EduRAG.Infrastructure.Persistence.Queries;

public class ChapterQueries : IChapterQueries
{
    private readonly IDbConnection _db;
    public ChapterQueries(IDbConnection db) => _db = db;

    public async Task<IEnumerable<ChapterDto>> GetBySubjectIdAsync(int subjectId)
    {
        const string sql = @"
            SELECT ""Id"", ""Title"", ""OrderIndex"", ""SubjectId"", ""IsActive""
            FROM ""Chapters""
            WHERE ""SubjectId"" = @subjectId AND ""IsActive"" = TRUE
            ORDER BY ""OrderIndex"", ""Title""";
        return await _db.QueryAsync<ChapterDto>(sql, new { subjectId });
    }
}
