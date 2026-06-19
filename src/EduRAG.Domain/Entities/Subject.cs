namespace EduRAG.Domain.Entities;

public class Subject
{
    public int      Id          { get; set; }
    public string   Name        { get; set; } = string.Empty;
    public string   Description { get; set; } = string.Empty;
    public int      ClassId     { get; set; }
    public bool     IsActive    { get; set; } = true;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    public Class                      Class     { get; set; } = null!;
    public ICollection<Chapter>       Chapters  { get; set; } = new List<Chapter>();
    public ICollection<StudyMaterial> Materials { get; set; } = new List<StudyMaterial>();
}
