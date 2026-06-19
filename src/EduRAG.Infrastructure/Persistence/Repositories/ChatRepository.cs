using EduRAG.Application.Interfaces;
using EduRAG.Domain.Entities;

namespace EduRAG.Infrastructure.Persistence.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly AppDbContext _ctx;
    public ChatRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<ChatSession> CreateSessionAsync(ChatSession session)
    {
        _ctx.ChatSessions.Add(session);
        await _ctx.SaveChangesAsync();
        return session;
    }

    public async Task<ChatSession?> GetSessionAsync(Guid sessionId)
        => await _ctx.ChatSessions.FindAsync(sessionId);

    public async Task<ChatMessage> AddMessageAsync(ChatMessage message)
    {
        _ctx.ChatMessages.Add(message);
        await _ctx.SaveChangesAsync();
        return message;
    }
}
