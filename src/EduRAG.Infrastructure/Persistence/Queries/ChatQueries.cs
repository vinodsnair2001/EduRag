using Dapper;
using EduRAG.Application.DTOs;
using EduRAG.Application.Interfaces;
using System.Data;

namespace EduRAG.Infrastructure.Persistence.Queries;

public class ChatQueries : IChatQueries
{
    private readonly IDbConnection _db;
    public ChatQueries(IDbConnection db) => _db = db;

    public async Task<IEnumerable<ChatMessageDto>> GetLastNMessagesAsync(Guid sessionId, int n)
    {
        const string sql = @"
            SELECT ""Id"", ""Content"", ""Role"", ""SentAt""
            FROM ""ChatMessages""
            WHERE ""SessionId"" = @sessionId
            ORDER BY ""SentAt"" DESC
            LIMIT @n";
        var msgs = await _db.QueryAsync<ChatMessageDto>(sql, new { sessionId, n });
        return msgs.Reverse();
    }

    public async Task<IEnumerable<ChatMessageDto>> GetSessionMessagesAsync(Guid sessionId)
    {
        const string sql = @"
            SELECT ""Id"", ""Content"", ""Role"", ""SentAt""
            FROM ""ChatMessages""
            WHERE ""SessionId"" = @sessionId
            ORDER BY ""SentAt"" ASC";
        return await _db.QueryAsync<ChatMessageDto>(sql, new { sessionId });
    }
}
