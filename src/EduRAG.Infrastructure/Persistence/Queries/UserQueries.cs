using Dapper;
using EduRAG.Application.DTOs;
using EduRAG.Application.Interfaces;
using System.Data;

namespace EduRAG.Infrastructure.Persistence.Queries;

public class UserQueries : IUserQueries
{
    private readonly IDbConnection _db;
    public UserQueries(IDbConnection db) => _db = db;

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        const string sql = @"
            SELECT ""Id"", ""Email"", ""FullName"", ""Role"", ""IsActive"", ""CreatedAt"", ""LastLoginAt"", ""ClassId""
            FROM ""AppUsers""
            ORDER BY ""CreatedAt"" DESC";
        return await _db.QueryAsync<UserDto>(sql);
    }
}
