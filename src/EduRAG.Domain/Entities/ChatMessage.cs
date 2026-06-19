using EduRAG.Domain.Enums;

namespace EduRAG.Domain.Entities;

public class ChatMessage
{
    public Guid        Id             { get; set; } = Guid.NewGuid();
    public Guid        SessionId      { get; set; }
    public string      Content        { get; set; } = string.Empty;
    public MessageRole Role           { get; set; }
    public DateTime    SentAt         { get; set; } = DateTime.UtcNow;
    public string?     SourceChunkIds { get; set; }
    public ChatSession Session        { get; set; } = null!;
}
