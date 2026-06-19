namespace EduRAG.Domain.Entities;

public class ChatSession
{
    public Guid      Id        { get; set; } = Guid.NewGuid();
    public Guid      UserId    { get; set; }
    public int       ClassId   { get; set; }
    public int       SubjectId { get; set; }
    public DateTime  StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt   { get; set; }
    public AppUser   User      { get; set; } = null!;
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
