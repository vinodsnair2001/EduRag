---
tags: [architecture, infrastructure, ef-core, dapper, repositories]
created: 2026-06-18
updated: 2026-06-19
type: architecture
status: stable
aliases: [Infrastructure Layer, EF Core, Dapper, Repositories]
---

# Infrastructure Layer

> [[_HOME|← Home]] · [[01-Clean-Architecture|← Clean Architecture]]

## Responsibilities

Implements all interfaces defined in Application. Divided into three areas:
1. **Persistence** — EF Core (writes) + Dapper (reads)
2. **Services** — Ollama AI, file storage, PDF processing
3. **Background Jobs** — vectorization worker

---

## Persistence

### AppDbContext

```csharp
// EduRAG.Infrastructure/Persistence/AppDbContext.cs
public class AppDbContext : DbContext
{
    public DbSet<Class>          Classes          => Set<Class>();
    public DbSet<Subject>        Subjects         => Set<Subject>();
    public DbSet<Chapter>        Chapters         => Set<Chapter>();
    public DbSet<StudyMaterial>  StudyMaterials   => Set<StudyMaterial>();
    public DbSet<MaterialChunk>  MaterialChunks   => Set<MaterialChunk>();
    public DbSet<AppUser>        AppUsers         => Set<AppUser>();
    public DbSet<ChatSession>    ChatSessions     => Set<ChatSession>();
    public DbSet<ChatMessage>    ChatMessages     => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        mb.HasPostgresExtension("vector");   // enables pgvector
    }
}
```

### MaterialChunkConfiguration (key: pgvector mapping)

Domain keeps `float[]` (no DB dependency in Domain). A value converter bridges to `Pgvector.Vector` at the EF Core boundary.

```csharp
// Maps float[] Embedding → PostgreSQL vector(768) via Pgvector.Vector value converter
public class MaterialChunkConfiguration : IEntityTypeConfiguration<MaterialChunk>
{
    public void Configure(EntityTypeBuilder<MaterialChunk> b)
    {
        b.ToTable("MaterialChunks");
        b.HasKey(x => x.Id);
        b.Property(x => x.Embedding)
            .HasConversion(
                v => new Vector(v),          // float[] → Vector (write to DB)
                v => v.Memory.ToArray(),     // Vector → float[] (read from DB)
                new ValueComparer<float[]>(  // required: EF change tracking
                    (a, b) => a != null && b != null && a.SequenceEqual(b),
                    v => v.Aggregate(0, (h, e) => HashCode.Combine(h, e.GetHashCode())),
                    v => v.ToArray()))
            .HasColumnType("vector(768)");
        b.HasIndex(x => new { x.ClassId, x.SubjectId });
        b.HasOne(x => x.Material)
         .WithMany(m => m.Chunks)
         .HasForeignKey(x => x.MaterialId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### EF Core Repositories (Write Operations Only)

Pattern is identical across all repositories:

```csharp
public class ClassRepository : IClassRepository
{
    private readonly AppDbContext _ctx;
    public ClassRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<Class> CreateAsync(Class entity) {
        _ctx.Classes.Add(entity);
        await _ctx.SaveChangesAsync();
        return entity;
    }
    public async Task<Class> UpdateAsync(Class entity) {
        _ctx.Classes.Update(entity);
        await _ctx.SaveChangesAsync();
        return entity;
    }
    public async Task DeleteAsync(int id) {
        var e = await _ctx.Classes.FindAsync(id) ?? throw new KeyNotFoundException();
        _ctx.Classes.Remove(e);
        await _ctx.SaveChangesAsync();
    }
}
```

Repositories: `ClassRepository`, `SubjectRepository`, `ChapterRepository`, `StudyMaterialRepository`, `MaterialChunkRepository`, `ChatRepository`.

### NpgsqlDataSource — Shared Between EF Core and Dapper

> **Critical:** EF Core and Dapper must share a single `NpgsqlDataSource` so that the pgvector `UseVector()` type mapping is registered on every connection. If Dapper uses a raw `new NpgsqlConnection(cs)`, it misses the Vector type handler and will throw `different vector dimensions` at runtime.

```csharp
// ServiceRegistration.cs
var dataSourceBuilder = new NpgsqlDataSourceBuilder(cs);
dataSourceBuilder.UseVector();              // register Pgvector type handler
var dataSource = dataSourceBuilder.Build();
services.AddSingleton(dataSource);

// EF Core uses the same data source
services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(dataSource, npg => npg.UseVector()));

// Dapper connection also comes from the same data source
services.AddScoped<IDbConnection>(_ => dataSource.CreateConnection());
```

### Dapper Query Classes (Read Operations Only)

```csharp
// EduRAG.Infrastructure/Persistence/Queries/ClassQueries.cs
// Raw SQL via Dapper — no EF change tracking, fastest reads
public class ClassQueries
{
    private readonly IDbConnection _db;

    public async Task<IEnumerable<ClassWithSubjectsDto>> GetAllWithSubjectsAsync()
    {
        const string sql = @"
            SELECT c.""Id"", c.""Name"", c.""Grade"",
                   s.""Id"" AS SubjectId, s.""Name"" AS SubjectName
            FROM ""Classes"" c
            LEFT JOIN ""Subjects"" s ON s.""ClassId"" = c.""Id""
            WHERE c.""IsActive"" = TRUE
            ORDER BY c.""Grade"", s.""Name""";
        // multi-map with Dapper splitOn
    }
}
```

Query classes: `ClassQueries`, `SubjectQueries`, `ChapterQueries`, `MaterialQueries`, `ChatQueries`.

---

## Services

### OllamaAIService

See [[07-AI-Pipeline]] for full implementation. Key points:
- `HttpClient` with `BaseAddress = http://localhost:11434`
- `POST /api/embeddings` → `float[768]`
- `POST /api/chat` with `stream: true` → `IAsyncEnumerable<string>`

### VectorSearchService

Dapper does not know how to serialize `Pgvector.Vector` objects — pass the embedding as a string literal and cast it in SQL with an explicit dimension. The explicit `::vector(768)` causes PostgreSQL to reject any dimension mismatch at cast time.

> **CRITICAL — locale bug:** Build the literal with `CultureInfo.InvariantCulture`. On a machine whose current culture uses a comma decimal separator, `float.ToString()` renders `0.5` as `0,5`. Since the literal is comma-delimited, Postgres then reads each decimal-comma as an element separator and parses **1536 tokens from a 768-element array** → `expected 768 dimensions, not 1536`. EF Core's `Pgvector.Vector` converter is immune (it serializes with invariant culture), which is why *storage* works while this manual *query* literal breaks.

```csharp
// Correct: invariant-culture float formatting + explicit ::vector(768) cast
var vectorLiteral = "[" +
    string.Join(",", queryEmbedding.Select(f => f.ToString("R", CultureInfo.InvariantCulture))) +
    "]";
const string sql = @"
    SELECT ""Id"" AS ChunkId, ""Content"", ""PageNumber"",
           1 - (""Embedding"" <=> @vector::vector(768)) AS Score
    FROM ""MaterialChunks""
    WHERE ""ClassId"" = @classId AND ""SubjectId"" = @subjectId
    ORDER BY ""Embedding"" <=> @vector::vector(768)
    LIMIT @topK";
return await _db.QueryAsync<ChunkSearchResult>(sql, new { vector = vectorLiteral, classId, subjectId, topK });
```

`ChunkSearchResult` constructor parameter order must match the SQL column order exactly (Dapper maps positionally):
```csharp
public record ChunkSearchResult(Guid ChunkId, string Content, int PageNumber, double Score);
//                                                              ^ PageNumber before Score
```

### PdfProcessingService

- Uses **PdfPig** (MIT, no cost)
- Sliding window chunking: 500 words per chunk, 50-word overlap
- Returns `List<(string Text, int PageNumber)>`

### LocalFileStorageService

- Stores files at `/storage/materials/{classId}/{subjectId}/{chapterId|general}/`
- Renames file on disk (GUID-based) to prevent path traversal
- Returns relative stored path saved in `StudyMaterial.StoredFilePath`

---

## Background Jobs

### VectorizationWorker

```csharp
// BackgroundService reads from Channel<Guid>
public class VectorizationWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var materialId in _queue.Reader.ReadAllAsync(ct))
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<VectorizationProcessor>();
            await processor.ProcessAsync(materialId, ct);
        }
    }
}
```

### VectorizationProcessor steps

```
1. Load StudyMaterial from DB
2. Set status = Processing
3. Check content hash → skip if already vectorized (dedup guard)
4. OpenRead() PDF from IFileStorageService
5. ExtractAndChunk() via PdfProcessingService
6. For each chunk: GetEmbeddingAsync() → float[768]
7. Build MaterialChunk entities
8. BulkInsertAsync() all chunks
9. Set status = Completed
On exception: set status = Failed, save error message
```

### PendingMaterialsRequeueService

`Channel<Guid>` is in-memory and empty on every API restart. Any material left in `Pending` or `Failed` status would never be processed without this startup service.

```csharp
// Runs once at startup — re-queues all Pending/Failed materials
public class PendingMaterialsRequeueService : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var ids = await db.StudyMaterials
            .Where(m => m.VectorizationStatus == VectorizationStatus.Pending
                     || m.VectorizationStatus == VectorizationStatus.Failed)
            .Select(m => m.Id)
            .ToListAsync(ct);

        foreach (var id in ids)
            await _queue.Writer.WriteAsync(id, ct);
    }
}
```

Registration order in `ServiceRegistration.cs` matters — `PendingMaterialsRequeueService` must be registered **after** `VectorizationWorker` so the worker is already reading from the channel when IDs are written:

```csharp
services.AddHostedService<VectorizationWorker>();
services.AddHostedService<PendingMaterialsRequeueService>();  // after worker
```

---

## Related Docs

- [[03-Application-Layer]] — interfaces being implemented
- [[07-AI-Pipeline]] — Ollama service detail
- [[../System/01-Database-Schema]] — DB tables being mapped
