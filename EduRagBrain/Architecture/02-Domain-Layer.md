---
tags: [architecture, domain, entities, enums]
created: 2026-06-18
updated: 2026-06-18
type: architecture
status: stable
aliases: [Domain Layer, Entities]
---

# Domain Layer

> [[_HOME|← Home]] · [[01-Clean-Architecture|← Clean Architecture]]

## Rules

- **Zero NuGet dependencies** — pure C# POCOs
- **No EF Core attributes** — all mapping in Infrastructure `IEntityTypeConfiguration<T>`
- **No framework references** — `System.*` only

---

## Entity Hierarchy

```
Class (1) ──────────────────────────────── (many) Subject
                                                      │
                                                      ├── (many) Chapter
                                                      │
                                                      └── (many) StudyMaterial
                                                                      │
                                                                  (many) MaterialChunk
                                                                         [vector(768)]

AppUser ── (many) ChatSession ── (many) ChatMessage
```

---

## Entities

### Class

```csharp
// EduRAG.Domain/Entities/Class.cs
public class Class {
    public int      Id        { get; set; }
    public string   Name      { get; set; }   // e.g. "Class 6"
    public int      Grade     { get; set; }   // 1–12
    public bool     IsActive  { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<Subject> Subjects { get; set; }
}
```

### Subject

```csharp
public class Subject {
    public int      Id          { get; set; }
    public string   Name        { get; set; }
    public string   Description { get; set; }
    public int      ClassId     { get; set; }   // FK → Class
    public bool     IsActive    { get; set; }
    public DateTime CreatedAt   { get; set; }
    public Class                      Class     { get; set; }
    public ICollection<Chapter>       Chapters  { get; set; }
    public ICollection<StudyMaterial> Materials { get; set; }
}
```

### Chapter

```csharp
public class Chapter {
    public int      Id         { get; set; }
    public string   Title      { get; set; }
    public int      OrderIndex { get; set; }   // display ordering
    public int      SubjectId  { get; set; }   // FK → Subject
    public bool     IsActive   { get; set; }
    public DateTime CreatedAt  { get; set; }
    public Subject                    Subject   { get; set; }
    public ICollection<StudyMaterial> Materials { get; set; }
}
```

### StudyMaterial

```csharp
public class StudyMaterial {
    public Guid     Id                 { get; set; }
    public string   OriginalFileName   { get; set; }
    public string   StoredFilePath     { get; set; }
    public string   ContentHash        { get; set; }   // SHA-256, used for dedup
    public long     FileSizeBytes      { get; set; }
    public int      ClassId            { get; set; }
    public int      SubjectId          { get; set; }
    public int?     ChapterId          { get; set; }   // nullable — can be subject-level
    public Guid     UploadedById       { get; set; }
    public DateTime UploadedAt         { get; set; }
    public VectorizationStatus VectorizationStatus { get; set; }
    public string?  VectorizationError { get; set; }
    public Subject  Subject  { get; set; }
    public Chapter? Chapter  { get; set; }
    public ICollection<MaterialChunk> Chunks { get; set; }
}
```

### MaterialChunk ← vector store row

```csharp
// One text window extracted from a PDF page, plus its 768-dim embedding.
// ClassId/SubjectId/ChapterId are denormalized for fast filtered vector search.
public class MaterialChunk {
    public Guid    Id         { get; set; }
    public Guid    MaterialId { get; set; }   // FK → StudyMaterial (CASCADE DELETE)
    public int     ClassId    { get; set; }   // denormalized
    public int     SubjectId  { get; set; }   // denormalized
    public int?    ChapterId  { get; set; }   // denormalized
    public string  Content    { get; set; }
    public int     ChunkIndex { get; set; }
    public int     PageNumber { get; set; }
    public float[] Embedding  { get; set; }  // mapped → vector(768) by EF config
    public StudyMaterial Material { get; set; }
}
```

### AppUser

```csharp
public class AppUser {
    public Guid      Id           { get; set; }
    public string    Email        { get; set; }
    public string    FullName     { get; set; }
    public string    PasswordHash { get; set; }   // BCrypt, work factor 11
    public UserRole  Role         { get; set; }
    public bool      IsActive     { get; set; }
    public DateTime  CreatedAt    { get; set; }
    public DateTime? LastLoginAt  { get; set; }
    public ICollection<ChatSession> ChatSessions { get; set; }
}
```

### ChatSession

```csharp
public class ChatSession {
    public Guid      Id        { get; set; }
    public Guid      UserId    { get; set; }    // FK → AppUser
    public int       ClassId   { get; set; }
    public int       SubjectId { get; set; }
    public DateTime  StartedAt { get; set; }
    public DateTime? EndedAt   { get; set; }
    public AppUser   User      { get; set; }
    public ICollection<ChatMessage> Messages { get; set; }
}
```

### ChatMessage

```csharp
public class ChatMessage {
    public Guid        Id             { get; set; }
    public Guid        SessionId      { get; set; }   // FK → ChatSession (CASCADE)
    public string      Content        { get; set; }
    public MessageRole Role           { get; set; }   // User | Assistant
    public DateTime    SentAt         { get; set; }
    public string?     SourceChunkIds { get; set; }   // JSON array of chunk Guids
    public ChatSession Session        { get; set; }
}
```

---

## Enums

```csharp
namespace EduRAG.Domain.Enums;

public enum UserRole            { Admin = 0, Student = 1 }
public enum MessageRole         { User = 0, Assistant = 1 }
public enum VectorizationStatus { Pending = 0, Processing = 1, Completed = 2, Failed = 3 }
```

Database stores enum as `INT`. `VectorizationStatus` drives the status badge in the admin UI.

---

## Domain Events (future)

Records in `EduRAG.Domain/Events/` — not yet wired to a dispatcher but scaffolded for future use:

```csharp
public record MaterialUploadedEvent(Guid MaterialId, int ClassId, int SubjectId);
public record VectorizationCompletedEvent(Guid MaterialId, int ChunkCount);
public record ChatSessionStartedEvent(Guid SessionId, Guid UserId);
```

---

## Related Docs

- [[03-Application-Layer]] — interfaces and use-cases that consume these entities
- [[../System/01-Database-Schema]] — PostgreSQL DDL that persists these entities
- [[04-Infrastructure-Layer]] — EF Core configurations that map these entities
