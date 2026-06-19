using EduRAG.Application.DTOs;
using EduRAG.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EduRAG.Infrastructure.Services;

public class OllamaAIService : IAIService
{
    private readonly HttpClient _http;
    private readonly string     _embedModel;
    private readonly string     _chatModel;

    public OllamaAIService(HttpClient http, IConfiguration config)
    {
        _http       = http;
        _embedModel = config["Ollama:EmbedModel"] ?? "nomic-embed-text";
        _chatModel  = config["Ollama:ChatModel"]  ?? "llama3.2";
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/embeddings",
            new { model = _embedModel, prompt = text }, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EmbedResponse>(cancellationToken: ct);
        return result!.Embedding;
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        string systemPrompt,
        IEnumerable<ChatMessageDto> history,
        string userMessage,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };
        foreach (var h in history)
            messages.Add(new { role = h.Role == EduRAG.Domain.Enums.MessageRole.User ? "user" : "assistant", content = h.Content });
        messages.Add(new { role = "user", content = userMessage });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = JsonContent.Create(new { model = _chatModel, messages, stream = true })
        };

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new System.IO.StreamReader(stream);
        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;
            var chunk = JsonSerializer.Deserialize<ChatStreamChunk>(line);
            if (chunk?.Message?.Content is not null)
                yield return chunk.Message.Content;
            if (chunk?.Done == true) break;
        }
    }

    private record EmbedResponse([property: JsonPropertyName("embedding")] float[] Embedding);
    private record ChatStreamChunk(
        [property: JsonPropertyName("message")] StreamMessage? Message,
        [property: JsonPropertyName("done")]    bool Done);
    private record StreamMessage([property: JsonPropertyName("content")] string? Content);
}
