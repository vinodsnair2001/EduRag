using EduRAG.API.Extensions;
using EduRAG.Application.DTOs;
using EduRAG.Application.Interfaces;
using EduRAG.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduRAG.API.Controllers;

[ApiController, Route("api/chat"), Authorize(Roles = "Student")]
public class ChatController : ControllerBase
{
    private readonly ChatUseCase  _chat;
    private readonly IChatQueries _chatQ;
    private readonly ILogger<ChatController> _logger;

    public ChatController(ChatUseCase chat, IChatQueries chatQ, ILogger<ChatController> logger)
    {
        _chat  = chat;
        _chatQ = chatQ;
        _logger = logger;
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest req)
    {
        var userId    = User.GetUserId();
        var sessionId = await _chat.CreateSessionAsync(userId, req.ClassId, req.SubjectId, req.ChapterIds ?? Array.Empty<int>());
        return Ok(new CreateSessionResponse(sessionId));
    }

    [HttpGet("sessions/{sessionId:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid sessionId)
        => Ok(await _chatQ.GetSessionMessagesAsync(sessionId));

    [HttpGet("diag"), AllowAnonymous]
    public async Task<IActionResult> Diag([FromServices] EduRAG.Application.Interfaces.IAIService ai,
                                          [FromServices] EduRAG.Application.Interfaces.IVectorSearchService vs)
    {
        var steps = new System.Collections.Generic.List<string>();
        try
        {
            steps.Add("embedding: start");
            var emb = await ai.GetEmbeddingAsync("hello", HttpContext.RequestAborted);
            steps.Add($"embedding: ok ({emb.Length} dims)");

            steps.Add("vector search: start");
            var chunks = (await vs.SearchAsync(emb, 1, 1, null)).ToList();
            steps.Add($"vector search: ok ({chunks.Count} chunks)");
        }
        catch (Exception ex)
        {
            steps.Add($"FAIL: {ex.GetType().Name}: {ex.Message}");
        }
        return Ok(steps);
    }

    [HttpPost("sessions/{sessionId:guid}/messages")]
    public async Task SendMessage(Guid sessionId, [FromBody] SendMessageRequest req, CancellationToken ct)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");

        await Response.WriteAsync(": connected\n\n", ct);
        await Response.Body.FlushAsync(ct);

        try
        {
            var userId = User.GetUserId();
            await foreach (var token in _chat.SendMessageAsync(sessionId, userId, req.Content, ct))

            {
                // Tokens from llama3.2 can contain newlines — encode as JSON string so
                // the SSE line stays on one line and the client can safely JSON.parse it.
                var escaped = System.Text.Json.JsonSerializer.Serialize(token);
                await Response.WriteAsync($"data: {escaped}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat stream error for session {SessionId}: {Type} {Message}", sessionId, ex.GetType().Name, ex.Message);
            try
            {
                var errorPayload = System.Text.Json.JsonSerializer.Serialize(new { error = $"{ex.GetType().Name}: {ex.Message}" });
                await Response.WriteAsync($"event: error\ndata: {errorPayload}\n\n");
                await Response.Body.FlushAsync();
            }
            catch { /* response may already be closed */ }
        }
    }
}
