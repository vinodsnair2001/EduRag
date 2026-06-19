using Dapper;
using EduRAG.Application.DTOs;
using EduRAG.Application.Interfaces;
using System.Data;

namespace EduRAG.Infrastructure.Persistence.Queries;

public class StudentPermissionQueries : IStudentPermissionQueries
{
    private readonly IDbConnection _db;
    public StudentPermissionQueries(IDbConnection db) => _db = db;

    public async Task<IEnumerable<StudentPermissionDto>> GetByStudentIdAsync(Guid studentId)
    {
        const string sql = @"
            SELECT sp.""Id"", sp.""StudentId"", sp.""SubjectId"",
                   s.""Name"" AS ""SubjectName"", sp.""GrantedAt""
            FROM ""StudentPermissions"" sp
            INNER JOIN ""Subjects"" s ON s.""Id"" = sp.""SubjectId""
            WHERE sp.""StudentId"" = @studentId
            ORDER BY s.""Name""";
        return await _db.QueryAsync<StudentPermissionDto>(sql, new { studentId });
    }

    public async Task<StudentClassDto?> GetStudentClassAsync(Guid studentId)
    {
        const string sql = @"
            SELECT c.""Id"" AS ""ClassId"", c.""Name"" AS ""ClassName"", c.""Grade""
            FROM ""AppUsers"" u
            INNER JOIN ""Classes"" c ON c.""Id"" = u.""ClassId""
            WHERE u.""Id"" = @studentId AND u.""ClassId"" IS NOT NULL";
        return await _db.QueryFirstOrDefaultAsync<StudentClassDto>(sql, new { studentId });
    }

    public async Task<IEnumerable<StudentSubjectDto>> GetPermittedSubjectsAsync(Guid studentId)
    {
        const string sql = @"
            SELECT s.""Id"" AS ""SubjectId"", s.""Name"" AS ""SubjectName"", s.""Description""
            FROM ""StudentPermissions"" sp
            INNER JOIN ""Subjects"" s ON s.""Id"" = sp.""SubjectId""
            WHERE sp.""StudentId"" = @studentId AND s.""IsActive"" = TRUE
            ORDER BY s.""Name""";
        return await _db.QueryAsync<StudentSubjectDto>(sql, new { studentId });
    }
}
