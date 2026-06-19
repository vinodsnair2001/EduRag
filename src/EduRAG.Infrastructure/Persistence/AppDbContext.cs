using EduRAG.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EduRAG.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly int _embeddingDimensions;

    public AppDbContext(DbContextOptions<AppDbContext> options, IConfiguration config)
        : base(options)
    {
        _embeddingDimensions = config.GetValue<int>("AI:EmbeddingDimensions", 768);
    }

    public DbSet<Class>          Classes        => Set<Class>();
    public DbSet<Subject>        Subjects       => Set<Subject>();
    public DbSet<Chapter>        Chapters       => Set<Chapter>();
    public DbSet<StudyMaterial>  StudyMaterials => Set<StudyMaterial>();
    public DbSet<MaterialChunk>  MaterialChunks => Set<MaterialChunk>();
    public DbSet<AppUser>        AppUsers       => Set<AppUser>();
    public DbSet<ChatSession>    ChatSessions   => Set<ChatSession>();
    public DbSet<ChatMessage>    ChatMessages   => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        mb.HasPostgresExtension("vector");

        // Override the vector column type after applying static configurations so the
        // dimension always matches AI:EmbeddingDimensions (768 = Ollama, 1024 = MistralAI).
        mb.Entity<MaterialChunk>()
          .Property(x => x.Embedding)
          .HasColumnType($"vector({_embeddingDimensions})");
    }
}
