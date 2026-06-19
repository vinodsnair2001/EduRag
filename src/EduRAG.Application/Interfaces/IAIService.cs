using EduRAG.Application.DTOs;

namespace EduRAG.Application.Interfaces;

public interface IAIService
{
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default);
    IAsyncEnumerable<string> StreamChatAsync(
        string systemPrompt,
        IEnumerable<ChatMessageDto> history,
        string userMessage,
        CancellationToken ct = default);
}
