using EduRAG.Application.DTOs;
using EduRAG.Application.Interfaces;
using EduRAG.Domain.Entities;
using EduRAG.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace EduRAG.Application.UseCases;

public class ChatUseCase
{
    private readonly IChatRepository      _chatRepo;
    private readonly IAIService           _ai;
    private readonly IVectorSearchService _vectorSearch;
    private readonly IChatQueries         _chatQueries;
    private readonly ISubjectQueries      _subjectQueries;
    private readonly ILogger<ChatUseCase> _logger;

    public ChatUseCase(
        IChatRepository chatRepo,
        IAIService ai,
        IVectorSearchService vectorSearch,
        IChatQueries chatQueries,
        ISubjectQueries subjectQueries,
        ILogger<ChatUseCase> logger)
    {
        _chatRepo       = chatRepo;
        _ai             = ai;
        _vectorSearch   = vectorSearch;
        _chatQueries    = chatQueries;
        _subjectQueries = subjectQueries;
        _logger         = logger;
    }

    public async Task<Guid> CreateSessionAsync(Guid userId, int classId, int subjectId)
    {
        var session = new ChatSession { UserId = userId, ClassId = classId, SubjectId = subjectId };
        var created = await _chatRepo.CreateSessionAsync(session);
        return created.Id;
    }

    public async IAsyncEnumerable<string> SendMessageAsync(
        Guid sessionId,
        Guid userId,
        string userContent,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        _logger.LogInformation("[Chat] Step 1: GetSession {SessionId}", sessionId);
        var session = await _chatRepo.GetSessionAsync(sessionId)
            ?? throw new KeyNotFoundException("Session not found.");

        if (session.UserId != userId)
            throw new UnauthorizedAccessException("Session does not belong to this user.");

        _logger.LogInformation("[Chat] Step 2: AddUserMessage");
        await _chatRepo.AddMessageAsync(new ChatMessage
        {
            SessionId = sessionId,
            Content   = userContent,
            Role      = MessageRole.User,
        });

        _logger.LogInformation("[Chat] Step 3: GetEmbedding");
        var embedding = await _ai.GetEmbeddingAsync(userContent, ct);

        _logger.LogInformation("[Chat] Step 4: VectorSearch classId={ClassId} subjectId={SubjectId}", session.ClassId, session.SubjectId);
        var chunks = (await _vectorSearch.SearchAsync(embedding, session.ClassId, session.SubjectId)).ToList();
        _logger.LogInformation("[Chat] Step 4 done: {Count} chunks found", chunks.Count);

        _logger.LogInformation("[Chat] Step 5: GetChatHistory");
        var history = await _chatQueries.GetLastNMessagesAsync(sessionId, 10);

        _logger.LogInformation("[Chat] Step 6: GetSubject {SubjectId}", session.SubjectId);
        var subject = await _subjectQueries.GetByIdAsync(session.SubjectId);

        _logger.LogInformation("[Chat] Step 7: StreamChat start");
        var systemPrompt = BuildSystemPrompt(chunks, session.ClassId, subject?.Name ?? "General");
        var fullResponse = new StringBuilder();

        await foreach (var token in _ai.StreamChatAsync(systemPrompt, history, userContent, ct))
        {
            fullResponse.Append(token);
            yield return token;
        }

        _logger.LogInformation("[Chat] Step 7 done: {Length} chars, saving assistant message", fullResponse.Length);
        var sourceIds = chunks.Select(c => c.ChunkId).ToList();
        await _chatRepo.AddMessageAsync(new ChatMessage
        {
            SessionId      = sessionId,
            Content        = fullResponse.ToString(),
            Role           = MessageRole.Assistant,
            SourceChunkIds = JsonSerializer.Serialize(sourceIds),
        });
        _logger.LogInformation("[Chat] Complete");
    }

    private static string BuildSystemPrompt(List<ChunkSearchResult> chunks, int classId, string subjectName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"You are an expert tutor for Class {classId}, subject \"{subjectName}\".");
        sb.AppendLine("You help students understand their study material deeply and clearly.");
        sb.AppendLine();
        sb.AppendLine("RULES:");
        sb.AppendLine("1. Answer ONLY based on the CONTEXT sections provided below.");
        sb.AppendLine("2. If not enough info in context, say: \"I couldn't find this in the uploaded study material.\"");
        sb.AppendLine($"3. Explain at a level appropriate for Class {classId} students.");
        sb.AppendLine("4. Use examples and analogies when helpful.");
        sb.AppendLine("5. Never make up facts not in the context.");
        sb.AppendLine();
        sb.AppendLine("CONTEXT FROM STUDY MATERIAL:");
        int i = 1;
        foreach (var chunk in chunks)
        {
            sb.AppendLine($"--- CHUNK {i++} (Page {chunk.PageNumber}) ---");
            sb.AppendLine(chunk.Content);
        }
        return sb.ToString();
    }
}
