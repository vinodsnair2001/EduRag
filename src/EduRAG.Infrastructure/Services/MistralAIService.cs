using EduRAG.Application.DTOs;
using EduRAG.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EduRAG.Infrastructure.Services;

public class MistralAIService : IAIService
{
    private readonly HttpClient _http;
    private readonly string     _embedModel;
    private readonly string     _chatModel;

    public MistralAIService(HttpClient http, IConfiguration config)
    {
        _http       = http;
        _embedModel = config["AI:MistralAI:EmbedModel"] ?? "mistral-embed";
        _chatModel  = config["AI:MistralAI:ChatModel"]  ?? "mistral-large-latest";

        var apiKey = config["AI:MistralAI:ApiKey"]
            ?? throw new InvalidOperationException(
                "AI:MistralAI:ApiKey is required when AI:Provider = \"MistralAI\".");

        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/v1/embeddings",
            new { model = _embedModel, input = new[] { text } }, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<MistralEmbedResponse>(cancellationToken: ct);
        return result!.Data[0].Embedding;
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
            messages.Add(new
            {
                role    = h.Role == EduRAG.Domain.Enums.MessageRole.User ? "user" : "assistant",
                content = h.Content
            });

        messages.Add(new { role = "user", content = userMessage });

        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = JsonContent.Create(new { model = _chatModel, messages, stream = true })
        };

        using var response = await _http.SendAsync(request,
            HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new System.IO.StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!line.StartsWith("data: ")) continue;

            var json = line["data: ".Length..];
            if (json == "[DONE]") break;

            var chunk = JsonSerializer.Deserialize<MistralStreamChunk>(json);
            var content = chunk?.Choices?[0]?.Delta?.Content;
            if (content is not null)
                yield return content;
        }
    }

    // ── Response models ──────────────────────────────────────────────────────

    private record MistralEmbedResponse(
        [property: JsonPropertyName("data")] List<EmbedData> Data);

    private record EmbedData(
        [property: JsonPropertyName("embedding")] float[] Embedding);

    private record MistralStreamChunk(
        [property: JsonPropertyName("choices")] List<StreamChoice>? Choices);

    private record StreamChoice(
        [property: JsonPropertyName("delta")]         StreamDelta? Delta,
        [property: JsonPropertyName("finish_reason")] string?      FinishReason);

    private record StreamDelta(
        [property: JsonPropertyName("content")] string? Content);
}
