namespace EduRAG.Domain.Entities;

public class Class
{
    public int      Id        { get; set; }
    public string   Name      { get; set; } = string.Empty;
    public int      Grade     { get; set; }
    public bool     IsActive  { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
