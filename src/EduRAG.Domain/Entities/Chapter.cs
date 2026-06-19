namespace EduRAG.Domain.Entities;

public class Chapter
{
    public int      Id         { get; set; }
    public string   Title      { get; set; } = string.Empty;
    public int      OrderIndex { get; set; }
    public int      SubjectId  { get; set; }
    public bool     IsActive   { get; set; } = true;
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
    public Subject                    Subject   { get; set; } = null!;
    public ICollection<StudyMaterial> Materials { get; set; } = new List<StudyMaterial>();
}
