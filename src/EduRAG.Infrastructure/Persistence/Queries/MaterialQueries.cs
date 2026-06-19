using Dapper;
using EduRAG.Application.DTOs;
using EduRAG.Application.Interfaces;
using EduRAG.Domain.Enums;
using System.Data;

namespace EduRAG.Infrastructure.Persistence.Queries;

public class MaterialQueries : IMaterialQueries
{
    private readonly IDbConnection _db;
    public MaterialQueries(IDbConnection db) => _db = db;

    public async Task<IEnumerable<MaterialDto>> GetAllAsync()
    {
        const string sql = @"
            SELECT ""Id"", ""OriginalFileName"", ""FileSizeBytes"",
                   ""VectorizationStatus"" AS Status, ""UploadedAt"",
                   ""VectorizationError"", ""ClassId"", ""SubjectId"", ""ChapterId""
            FROM ""StudyMaterials""
            ORDER BY ""UploadedAt"" DESC";
        return await _db.QueryAsync<MaterialDto>(sql);
    }

    public async Task<IEnumerable<MaterialDto>> GetBySubjectAsync(int subjectId)
    {
        const string sql = @"
            SELECT ""Id"", ""OriginalFileName"", ""FileSizeBytes"",
                   ""VectorizationStatus"" AS Status, ""UploadedAt"",
                   ""VectorizationError"", ""ClassId"", ""SubjectId"", ""ChapterId""
            FROM ""StudyMaterials""
            WHERE ""SubjectId"" = @subjectId
            ORDER BY ""UploadedAt"" DESC";
        return await _db.QueryAsync<MaterialDto>(sql, new { subjectId });
    }
}
