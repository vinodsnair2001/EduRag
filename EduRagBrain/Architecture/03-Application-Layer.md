---
tags: [architecture, application, use-cases, interfaces, dtos]
created: 2026-06-18
updated: 2026-06-18
type: architecture
status: stable
aliases: [Application Layer, Use Cases, Ports]
---

# Application Layer

> [[_HOME|← Home]] · [[01-Clean-Architecture|← Clean Architecture]]

## Responsibilities

- Define **use-cases** (application business logic)
- Define **interfaces (ports)** that Infrastructure must implement
- Define **DTOs** for request/response shaping
- Depend only on `EduRAG.Domain` — never on Infrastructure or API

---

## Interfaces (Ports)

### IAIService

```csharp
// EduRAG.Application/Interfaces/IAIService.cs
public interface IAIService
{
    // Embed text via nomic-embed-text → 768-dim float[]
    Task<float[]> GetEmbeddingAsync(string text);

    // Stream chat tokens from llama3.2 via Ollama
    IAsyncEnumerable<string> StreamChatAsync(
        string systemPrompt,
        IEnumerable<ChatMessageDto> history,
        string userMessage);
}
```

### IVectorSearchService

```csharp
public interface IVectorSearchService
{
    // Cosine similarity search scoped to classId + subjectId
    Task<IEnumerable<ChunkSearchResult>> SearchAsync(
        float[] queryEmbedding, int classId, int subjectId, int topK = 5);
}

public record ChunkSearchResult(Guid ChunkId, string Content, double Score, int PageNumber);
```

### IFileStorageService

```csharp
public interface IFileStorageService
{
    // Saves to: /storage/materials/{classId}/{subjectId}/{chapterId|'general'}/
    Task<string> SaveAsync(Stream fileStream, string fileName,
                           int classId, int subjectId, int? chapterId);
    Task DeleteAsync(string storedPath);
    Stream OpenRead(string storedPath);
}
```

### Repository Interfaces

```csharp
// Write interfaces only — Dapper queries are concrete classes in Infrastructure

public interface IClassRepository {
    Task<Class>   CreateAsync(Class entity);
    Task<Class>   UpdateAsync(Class entity);
    Task          DeleteAsync(int id);
}

public interface ISubjectRepository {
    Task<Subject> CreateAsync(Subject entity);
    Task<Subject> UpdateAsync(Subject entity);
    Task          DeleteAsync(int id);
}

public interface IChapterRepository {
    Task<Chapter> CreateAsync(Chapter entity);
    Task<Chapter> UpdateAsync(Chapter entity);
    Task          DeleteAsync(int id);
}

public interface IStudyMaterialRepository {
    Task<StudyMaterial>  CreateAsync(StudyMaterial entity);
    Task                 UpdateStatusAsync(Guid id, VectorizationStatus status, string? error = null);
    Task                 DeleteAsync(Guid id);
    Task<StudyMaterial?> GetByHashAsync(string contentHash);   // SHA-256 dedup check
}

public interface IMaterialChunkRepository {
    Task BulkInsertAsync(IEnumerable<MaterialChunk> chunks);
    Task DeleteByMaterialAsync(Guid materialId);
}

public interface IChatRepository {
    Task<ChatSession>  CreateSessionAsync(ChatSession session);
    Task<ChatMessage>  AddMessageAsync(ChatMessage message);
}
```

---

## Use-Cases

### ManageClassUseCase

Operations: `CreateClass`, `UpdateClass`, `DeleteClass`.
Uses `IClassRepository` (write) and returns result DTOs.

### ManageSubjectUseCase

Operations: `CreateSubject`, `UpdateSubject`, `DeleteSubject`.
Validates `ClassId` exists before creation.

### ManageChapterUseCase

Operations: `CreateChapter`, `UpdateChapter`, `DeleteChapter`, `ReorderChapters`.

### UploadMaterialUseCase

```
1. Validate file (PDF, ≤50 MB)
2. Compute SHA-256 hash
3. Call IStudyMaterialRepository.GetByHashAsync() → reject duplicate
4. Call IFileStorageService.SaveAsync()
5. Create StudyMaterial record (status = Pending)
6. Enqueue materialId → Channel<Guid>
7. Return material ID + status
```

### ChatUseCase (RAG Pipeline)

```
1. Validate session ownership (SessionId → UserId match)
2. Save user ChatMessage (EF Core)
3. GetEmbeddingAsync(userMessage) → float[768]
4. SearchAsync(embedding, classId, subjectId, topK=5) → List<ChunkSearchResult>
5. Build system prompt with chunk context (see RAG prompts)
6. Load last 10 messages from DB (Dapper ChatQueries)
7. StreamChatAsync(prompt, history, userMessage) → IAsyncEnumerable<string>
8. Write each token to HTTP response as SSE: "data: {token}\n\n"
9. After stream ends: save assistant ChatMessage + SourceChunkIds
```

---

## DTOs

```csharp
// Auth
public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, string Role, string FullName, Guid UserId);
public record RegisterRequest(string Email, string FullName, string Password, UserRole Role);

// Class/Subject/Chapter
public record CreateClassRequest(string Name, int Grade);
public record ClassDto(int Id, string Name, int Grade, List<SubjectDto> Subjects);
public record SubjectDto(int Id, string Name, int ClassId);
public record ChapterDto(int Id, string Title, int OrderIndex, int SubjectId);

// Materials
public record UploadMaterialResponse(Guid MaterialId, VectorizationStatus Status);
public record MaterialDto(Guid Id, string OriginalFileName, long FileSizeBytes,
                          VectorizationStatus Status, DateTime UploadedAt);

// Chat
public record SendMessageRequest(string Content);
public record ChatMessageDto(Guid Id, string Content, MessageRole Role, DateTime SentAt);
public record CreateSessionResponse(Guid SessionId);
```

---

## Common Utilities

```csharp
// Result<T> — avoids throwing for expected failures
public class Result<T> {
    public bool    IsSuccess { get; }
    public T?      Value     { get; }
    public string? Error     { get; }
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}

// PaginatedList<T>
public class PaginatedList<T> {
    public List<T> Items       { get; }
    public int     TotalCount  { get; }
    public int     Page        { get; }
    public int     PageSize    { get; }
    public int     TotalPages  => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
```

---

## Related Docs

- [[02-Domain-Layer]] — entities used in interfaces and DTOs
- [[04-Infrastructure-Layer]] — implementations of these interfaces
- [[05-API-Layer]] — controllers that call these use-cases
