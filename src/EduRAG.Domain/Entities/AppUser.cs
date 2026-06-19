using EduRAG.Domain.Enums;

namespace EduRAG.Domain.Entities;

public class AppUser
{
    public Guid      Id           { get; set; } = Guid.NewGuid();
    public string    Email        { get; set; } = string.Empty;
    public string    FullName     { get; set; } = string.Empty;
    public string    PasswordHash { get; set; } = string.Empty;
    public UserRole  Role         { get; set; }
    public int?      ClassId      { get; set; }   // null for Admin; required for Student
    public bool      IsActive     { get; set; } = true;
    public DateTime  CreatedAt    { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt  { get; set; }
    public Class?    Class        { get; set; }
    public ICollection<ChatSession>        ChatSessions       { get; set; } = new List<ChatSession>();
    public ICollection<StudentPermission>  SubjectPermissions { get; set; } = new List<StudentPermission>();
}
