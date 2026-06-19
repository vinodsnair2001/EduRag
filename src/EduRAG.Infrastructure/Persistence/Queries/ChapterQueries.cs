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
            SELECT c.""Id"", c.""Title"", c.""OrderIndex"", c.""SubjectId"", c.""IsActive"",
                   EXISTS (SELECT 1 FROM ""StudyMaterials"" sm WHERE sm.""ChapterId"" = c.""Id"") AS ""HasPdf""
            FROM ""Chapters"" c
            WHERE c.""SubjectId"" = @subjectId AND c.""IsActive"" = TRUE
            ORDER BY c.""OrderIndex"", c.""Title""";
        return await _db.QueryAsync<ChapterDto>(sql, new { subjectId });
    }
}
