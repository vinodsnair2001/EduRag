namespace EduRAG.Domain.Entities;

public class StudentPermission
{
    public Guid     Id        { get; set; } = Guid.NewGuid();
    public Guid     StudentId { get; set; }
    public int      SubjectId { get; set; }
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public AppUser  Student   { get; set; } = null!;
    public Subject  Subject   { get; set; } = null!;
}
