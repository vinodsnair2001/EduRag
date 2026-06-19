---
tags: [architecture, api, controllers, middleware, di]
created: 2026-06-18
updated: 2026-06-18
type: architecture
status: stable
aliases: [API Layer, Controllers]
---

# API Layer

> [[_HOME|← Home]] · [[01-Clean-Architecture|← Clean Architecture]]

## Controllers

### AuthController — `/api/auth`

| Method | Path | Auth | Action |
|--------|------|------|--------|
| POST | `/login` | None | Validate email+password (BCrypt), return JWT |
| POST | `/register` | Admin | Create Admin or Student account |

```csharp
[ApiController, Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)

    [HttpPost("register"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
}
```

### AdminController — `/api/admin`

All routes require `[Authorize(Roles = "Admin")]`.

```
GET    /classes                              → ClassQueries (Dapper)
POST   /classes                              → ManageClassUseCase.CreateClass
PUT    /classes/{id}                         → ManageClassUseCase.UpdateClass
DELETE /classes/{id}                         → ManageClassUseCase.DeleteClass

GET    /classes/{classId}/subjects           → SubjectQueries (Dapper)
POST   /classes/{classId}/subjects           → ManageSubjectUseCase.CreateSubject
PUT    /subjects/{id}                        → ManageSubjectUseCase.UpdateSubject
DELETE /subjects/{id}                        → ManageSubjectUseCase.DeleteSubject

GET    /subjects/{subjectId}/chapters        → ChapterQueries (Dapper)
POST   /subjects/{subjectId}/chapters        → ManageChapterUseCase.CreateChapter
PUT    /chapters/{id}                        → ManageChapterUseCase.UpdateChapter
DELETE /chapters/{id}                        → ManageChapterUseCase.DeleteChapter

POST   /upload  [multipart/form-data, 50MB]  → UploadMaterialUseCase
GET    /materials                            → MaterialQueries (Dapper)
DELETE /materials/{id}                       → UploadMaterialUseCase.Delete
```

### StudentController — `/api/student`

Requires `[Authorize(Roles = "Student")]`.

```
GET  /classes                               → ClassQueries (Dapper)
GET  /classes/{classId}/subjects            → SubjectQueries (Dapper)
GET  /subjects/{subjectId}/chapters         → ChapterQueries (Dapper)
```

### ChatController — `/api/chat`

Requires `[Authorize(Roles = "Student")]`.

```
POST /sessions                              → ChatRepository.CreateSession
GET  /sessions/{sessionId}/messages         → ChatQueries (Dapper)
POST /sessions/{sessionId}/messages         → ChatUseCase (SSE stream)
```

**Streaming endpoint detail:**
```csharp
[HttpPost("sessions/{sessionId}/messages")]
public async Task SendMessage(Guid sessionId, [FromBody] SendMessageRequest req, CancellationToken ct)
{
    Response.Headers.Append("Content-Type", "text/event-stream");
    Response.Headers.Append("Cache-Control", "no-cache");
    // ChatUseCase.ExecuteAsync() → yields tokens → write "data: {token}\n\n"
}
```

---

## Middleware

### JWT Authentication

```csharp
// Validates Bearer token on every request
// Reads claims: userId, role
// Rejects with 401 if token is expired / invalid
```

### Global Exception Handler

```csharp
// Catches all unhandled exceptions
// Returns structured JSON:
// { "statusCode": 500, "message": "...", "traceId": "..." }
// Logs full stack trace via ILogger
```

---

## DI Registration (ServiceRegistration.cs)

```csharp
public static class ServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection s) =>
        s.AddScoped<ManageClassUseCase>()
         .AddScoped<ManageSubjectUseCase>()
         .AddScoped<ManageChapterUseCase>()
         .AddScoped<UploadMaterialUseCase>()
         .AddScoped<ChatUseCase>();

    public static IServiceCollection AddInfrastructure(this IServiceCollection s, IConfiguration config)
    {
        // EF Core + pgvector
        s.AddDbContext<AppDbContext>(o => o.UseNpgsql(cs, npg => npg.UseVector()));
        s.AddScoped<IDbConnection>(_ => new NpgsqlConnection(cs));

        // Repositories (EF Core — writes)
        s.AddScoped<IClassRepository, ClassRepository>()
         .AddScoped<ISubjectRepository, SubjectRepository>()
         /* ... */;

        // Queries (Dapper — reads)
        s.AddScoped<ClassQueries>().AddScoped<SubjectQueries>() /* ... */;

        // AI
        s.AddHttpClient<IAIService, OllamaAIService>(c =>
            c.BaseAddress = new Uri(config["Ollama:BaseUrl"]!));
        s.AddScoped<IVectorSearchService, VectorSearchService>();
        s.AddScoped<PdfProcessingService>();
        s.AddScoped<VectorizationProcessor>();

        // File storage
        s.AddScoped<IFileStorageService, LocalFileStorageService>();

        // Background queue
        s.AddSingleton(Channel.CreateUnbounded<Guid>());
        s.AddHostedService<VectorizationWorker>();
        return s;
    }

    public static IServiceCollection AddAuth(this IServiceCollection s, IConfiguration config)
    {
        s.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
         .AddJwtBearer(o => { /* token validation parameters */ });
        return s;
    }
}
```

---

## Request Validation

- `[Required]`, `[MaxLength]`, `[Range]` on all DTOs
- Custom validator: file upload checks MIME type + extension (`.pdf` only)
- Global `ValidationProblemDetails` response on 400

---

## Related Docs

- [[03-Application-Layer]] — use-cases being called
- [[../System/02-API-Reference]] — full endpoint reference table
- [[../System/03-Security]] — auth and role details
