using System.Net;
using System.Text.Json;

namespace EduRAG.API.Middleware;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteErrorAsync(ctx, 404, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteErrorAsync(ctx, 403, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await WriteErrorAsync(ctx, 400, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AI service error: {Message}", ex.Message);
            var msg = ex.Message.Contains("model") || ex.StatusCode == HttpStatusCode.NotFound
                ? "AI model not available. Please ensure Ollama models are pulled (ollama pull llama3.2)."
                : "Could not reach the AI service. Please ensure Ollama is running on port 11434.";
            await WriteErrorAsync(ctx, 503, msg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await WriteErrorAsync(ctx, 500, "An internal error occurred.", ctx.TraceIdentifier);
        }
    }

    private static async Task WriteErrorAsync(HttpContext ctx, int status, string message, string? traceId = null)
    {
        ctx.Response.StatusCode  = status;
        ctx.Response.ContentType = "application/json";
        var body = JsonSerializer.Serialize(new { statusCode = status, message, traceId });
        await ctx.Response.WriteAsync(body);
    }
}
