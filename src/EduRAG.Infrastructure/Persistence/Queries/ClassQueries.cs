using Dapper;
using EduRAG.Application.DTOs;
using EduRAG.Application.Interfaces;
using System.Data;

namespace EduRAG.Infrastructure.Persistence.Queries;

public class ClassQueries : IClassQueries
{
    private readonly IDbConnection _db;
    public ClassQueries(IDbConnection db) => _db = db;

    public async Task<IEnumerable<ClassDto>> GetAllActiveAsync()
    {
        const string sql = @"
            SELECT ""Id"", ""Name"", ""Grade"", ""IsActive"", ""CreatedAt""
            FROM ""Classes""
            WHERE ""IsActive"" = TRUE
            ORDER BY ""Grade"", ""Name""";
        return await _db.QueryAsync<ClassDto>(sql);
    }

    public async Task<ClassDto?> GetByIdAsync(int classId)
    {
        const string sql = @"
            SELECT ""Id"", ""Name"", ""Grade"", ""IsActive"", ""CreatedAt""
            FROM ""Classes""
            WHERE ""Id"" = @classId";
        return await _db.QueryFirstOrDefaultAsync<ClassDto>(sql, new { classId });
    }
}
