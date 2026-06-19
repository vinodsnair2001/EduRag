using EduRAG.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduRAG.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Class>          Classes        => Set<Class>();
    public DbSet<Subject>        Subjects       => Set<Subject>();
    public DbSet<Chapter>        Chapters       => Set<Chapter>();
    public DbSet<StudyMaterial>  StudyMaterials => Set<StudyMaterial>();
    public DbSet<MaterialChunk>  MaterialChunks => Set<MaterialChunk>();
    public DbSet<AppUser>        AppUsers       => Set<AppUser>();
    public DbSet<ChatSession>         ChatSessions        => Set<ChatSession>();
    public DbSet<ChatMessage>         ChatMessages        => Set<ChatMessage>();
    public DbSet<StudentPermission>   StudentPermissions  => Set<StudentPermission>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        mb.HasPostgresExtension("vector");
    }
}
