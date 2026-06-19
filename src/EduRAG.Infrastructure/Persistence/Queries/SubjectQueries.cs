using Dapper;
using EduRAG.Application.DTOs;
using EduRAG.Application.Interfaces;
using System.Data;

namespace EduRAG.Infrastructure.Persistence.Queries;

public class SubjectQueries : ISubjectQueries
{
    private readonly IDbConnection _db;
    public SubjectQueries(IDbConnection db) => _db = db;

    public async Task<IEnumerable<SubjectDto>> GetByClassIdAsync(int classId)
    {
        const string sql = @"
            SELECT ""Id"", ""Name"", ""Description"", ""ClassId"", ""IsActive""
            FROM ""Subjects""
            WHERE ""ClassId"" = @classId AND ""IsActive"" = TRUE
            ORDER BY ""Name""";
        return await _db.QueryAsync<SubjectDto>(sql, new { classId });
    }

    public async Task<IEnumerable<SubjectDto>> GetByClassIdForStudentAsync(int classId, Guid studentId)
    {
        // Alias the outer table so the correlated EXISTS can reference s."Id" unambiguously.
        // Without the alias, PostgreSQL resolves "Id" to StudentPermissions."Id" (Guid) instead
        // of Subjects."Id" (int), making the EXISTS always false when permissions are set.
        const string sql = @"
            SELECT s.""Id"", s.""Name"", s.""Description"", s.""ClassId"", s.""IsActive""
            FROM ""Subjects"" s
            WHERE s.""ClassId"" = @classId AND s.""IsActive"" = TRUE
              AND (
                NOT EXISTS (SELECT 1 FROM ""StudentPermissions"" WHERE ""StudentId"" = @studentId)
                OR EXISTS  (SELECT 1 FROM ""StudentPermissions"" WHERE ""StudentId"" = @studentId AND ""SubjectId"" = s.""Id"")
              )
            ORDER BY s.""Name""";
        return await _db.QueryAsync<SubjectDto>(sql, new { classId, studentId });
    }

    public async Task<SubjectDto?> GetByIdAsync(int subjectId)
    {
        const string sql = @"
            SELECT ""Id"", ""Name"", ""Description"", ""ClassId"", ""IsActive""
            FROM ""Subjects""
            WHERE ""Id"" = @subjectId";
        return await _db.QueryFirstOrDefaultAsync<SubjectDto>(sql, new { subjectId });
    }
}
