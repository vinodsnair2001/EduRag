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

    public async Task<SubjectDto?> GetByIdAsync(int subjectId)
    {
        const string sql = @"
            SELECT ""Id"", ""Name"", ""Description"", ""ClassId"", ""IsActive""
            FROM ""Subjects""
            WHERE ""Id"" = @subjectId";
        return await _db.QueryFirstOrDefaultAsync<SubjectDto>(sql, new { subjectId });
    }
}
