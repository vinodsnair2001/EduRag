namespace EduRAG.Domain.Entities;

public class ChatSession
{
    public Guid      Id        { get; set; } = Guid.NewGuid();
    public Guid      UserId    { get; set; }
    public int       ClassId   { get; set; }
    public int       SubjectId { get; set; }
    public DateTime  StartedAt          { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt            { get; set; }
    // JSON int array e.g. "[1,2,3]". Empty string/null = no chapter filter (all chunks for the subject).
    public string?   SelectedChapterIds { get; set; }
    public AppUser   User               { get; set; } = null!;
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
