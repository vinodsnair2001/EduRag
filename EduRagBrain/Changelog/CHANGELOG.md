---
tags: [changelog, history]
created: 2026-06-18
updated: 2026-06-19
type: changelog
status: stable
aliases: [Changelog, Change History]
---

# Changelog

> [[_HOME|← Home]]

All changes to the EduRAG codebase and documentation are recorded here.

---

## 2026-06-19 — B-004: EF Core Vector Dimension Hardcode + Missing MistralAI Provider

**Type:** Fix
**Branch:** `feature-Vectorisation-based-on-class-subject-chapter`
**Affected:** Infrastructure

### What changed

**Root cause:** `MaterialChunkConfiguration` had `HasColumnType("vector(768)")` hardcoded. When the active AI provider is MistralAI (1024-dim embeddings), the DB column is `vector(1024)` but EF Core was generating INSERT parameters typed as `vector(768)`, causing PostgreSQL to reject with `expected 1024 dimensions, not 768` on every VectorizationWorker bulk-insert.

Second issue: `ServiceRegistration.cs` always registered `OllamaAIService` regardless of the `AI:Provider` config value. `MistralAIService.cs` was missing from this branch (it exists only on `main`).

**Fixes applied:**

1. `AppDbContext` now accepts `IConfiguration` in its constructor and overrides the `Embedding` column type in `OnModelCreating` after `ApplyConfigurationsFromAssembly`, using `AI:EmbeddingDimensions` (default 768). This means the column type always matches the configured provider dimension — no code change required when switching providers.

2. `AppDbContextFactory` updated to build `IConfiguration` from appsettings files and pass it to `AppDbContext`, keeping design-time migration tools working.

3. `MistralAIService.cs` ported from `main` — implements `IAIService` using `mistral-embed` (1024-dim) for embeddings and `mistral-large-latest` for streaming chat via the Mistral REST API.

4. `ServiceRegistration.cs` updated with conditional AI provider registration: reads `AI:Provider` at startup and registers either `MistralAIService` or `OllamaAIService`.

5. `EduRAG.Infrastructure.csproj` — added `Microsoft.Extensions.Configuration.Json` package (required by `AppDbContextFactory` to load appsettings at design time).

### Files changed

| File | Change |
|------|--------|
| `EduRAG.Infrastructure/Persistence/AppDbContext.cs` | Accept `IConfiguration`; override vector column type dynamically |
| `EduRAG.Infrastructure/Persistence/AppDbContextFactory.cs` | Build config from appsettings; pass to context |
| `EduRAG.Infrastructure/Services/MistralAIService.cs` | New — Mistral REST client for embeddings + streaming chat |
| `EduRAG.Infrastructure/ServiceRegistration.cs` | Conditional Ollama / MistralAI registration |
| `EduRAG.Infrastructure/EduRAG.Infrastructure.csproj` | + `Microsoft.Extensions.Configuration.Json` |

---

## 2026-06-19 — F-005: Chapter-Based Vectorisation & Student Chapter Selection

**Type:** Feature
**Branch:** `feature-Vectorisation-based-on-class-subject-chapter`
**Affected:** Backend (Domain, Application, Infrastructure, API), Frontend (Student portal, Admin portal), Docs

### What changed

**Backend**
- `ChatSession` entity — added `SelectedChapterIds string?` (JSON int array persisted to DB)
- `CreateSessionRequest` DTO — added `ChapterIds int[]` field
- `IVectorSearchService.SearchAsync` — added `int[]? chapterIds` parameter
- `VectorSearchService` — SQL WHERE clause dynamically adds `AND "ChapterId" = ANY(@chapterIds)` when chapter IDs are present
- `ChatUseCase.CreateSessionAsync` — serializes chapter IDs into `SelectedChapterIds`
- `ChatUseCase.SendMessageAsync` — deserializes chapter IDs from session and passes to vector search
- `ChatController` — forwards `ChapterIds` from `CreateSessionRequest` to use case
- New EF migration: `20260619204900_AddChapterIdsToChatSessions` — adds `SelectedChapterIds TEXT NULL` to `ChatSessions`; adds composite index `(ClassId, SubjectId, ChapterId)` on `MaterialChunks`

**Frontend — Student portal**
- `ClassSubjectSelectPage` — redesigned as a three-step wizard: (1) class cards, (2) subject cards, (3) chapter checkboxes with "Select All" toggle and sticky "Start N chapters" button
- `ChatPage` — reads `chapterIds` and `chapterTitles` from location state; passes `chapterIds` to `POST /chat/sessions`; shows selected chapter names in the header and welcome message

**Frontend — Admin portal**
- `MaterialListPage` — added chapter selector (third dropdown, optional) that fetches chapters when a subject is selected; passes `chapterId` to upload form; shows "Chapter assigned" / "Subject-level" label per material row; advisory warning when no chapter selected

**Docs**
- `EduRagBrain/Backlog/BACKLOG.md` — created; F-005 added with full task checklist
- `EduRagBrain/System/01-Database-Schema.md` — updated `ChatSessions` DDL; added new composite index
- `EduRagBrain/System/02-API-Reference.md` — updated `POST /chat/sessions` with `chapterIds` field
- `EduRagBrain/Architecture/07-AI-Pipeline.md` — updated vector search SQL to show chapter filter
- `EduRagBrain/Architecture/02-Domain-Layer.md` — updated `ChatSession` entity with `SelectedChapterIds`
- `EduRagBrain/Architecture/06-Frontend.md` — updated student UX principles and component map
- `EduRagBrain/User/01-Admin-Guide.md` — updated upload instructions; chapter vs. subject-level distinction

### Files changed

| File | Change |
|------|--------|
| `EduRAG.Domain/Entities/ChatSession.cs` | + `SelectedChapterIds` |
| `EduRAG.Application/DTOs/ChatDtos.cs` | `CreateSessionRequest` + `ChapterIds` |
| `EduRAG.Application/Interfaces/IVectorSearchService.cs` | + `chapterIds` param |
| `EduRAG.Application/UseCases/ChatUseCase.cs` | chapter IDs end-to-end |
| `EduRAG.Infrastructure/Services/VectorSearchService.cs` | chapter SQL filter |
| `EduRAG.Infrastructure/Migrations/20260619204900_AddChapterIdsToChatSessions.cs` | new migration |
| `EduRAG.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` | snapshot updated |
| `EduRAG.API/Controllers/ChatController.cs` | forward `ChapterIds` |
| `frontend/src/admin/pages/MaterialListPage.tsx` | chapter dropdown |
| `frontend/src/student/pages/ClassSubjectSelectPage.tsx` | three-step wizard |
| `frontend/src/student/pages/ChatPage.tsx` | chapter IDs to session + header |

---

## 2026-06-18 — Initial Knowledge Base Creation

**Type:** Docs
**Affected:** Docs

### What changed
- Created EduRagBrain Obsidian vault from Technical Specification v1.0
- Established full documentation structure: Architecture, System, User, Development, Skills, Changelog
- Architecture docs: 00-Overview, 01-Clean-Architecture, 02-Domain-Layer, 03-Application-Layer, 04-Infrastructure-Layer, 05-API-Layer, 06-Frontend, 07-AI-Pipeline
- System docs: 00-System-Overview, 01-Database-Schema, 02-API-Reference, 03-Security, 04-Configuration, 05-Deployment, 06-Troubleshooting
- User docs: 00-Getting-Started, 01-Admin-Guide, 02-Student-Guide, 03-FAQ
- Development docs: 00-Build-Order, 01-Setup-Guide, 02-Testing
- Skills docs: edurag-claude-skill, change-tracker

### Docs updated
- All docs created fresh from specification

---

## 2026-06-18 — Frontend Design Update: shadcn/ui + Children/Teen Theme

**Type:** Change
**Affected:** Frontend

### What changed
- Frontend stack updated to include **shadcn/ui** (Radix UI + Tailwind) as primary component library
- Student portal design philosophy updated: playful, age-appropriate for children and teenagers (8–18)
- Design tokens defined: Violet 500 primary, Nunito display font, Inter body font, rounded-2xl cards
- shadcn/ui component list specified for project use
- Added student UX principles: class card gradients by grade band, animated practice cards, star ratings, typing indicator
- New component added: `<ScoreAnimation />` (confetti/star burst on correct answers)

### Docs updated
- [[../Architecture/06-Frontend]] — complete rewrite of tech stack, design tokens, shadcn component list, student UX principles

---

## 2026-06-18 — Full Project Build: Backend + Frontend + Database

**Type:** Feature
**Affected:** Backend, Frontend, Database, Config

### What changed

**Backend — 4-Project .NET 8 Clean Architecture solution**
- `EduRAG.Domain` — entities (Class, Subject, Chapter, StudyMaterial, MaterialChunk, AppUser, ChatSession, ChatMessage), enums, events
- `EduRAG.Application` — Result<T>, DTOs, repository/query/service interfaces, use cases (Auth, ManageClass, ManageSubject, ManageChapter, UploadMaterial, Chat/RAG)
- `EduRAG.Infrastructure` — EF Core (writes) + Dapper (reads), OllamaAIService (streaming), JwtService, LocalFileStorageService, VectorSearchService, VectorizationWorker + Processor (BackgroundService + Channel<Guid> Singleton), PdfProcessingService (PdfPig, sliding window 500/50)
- `EduRAG.API` — controllers (Auth, Admin, Student, Chat), GlobalExceptionHandler middleware, JWT Bearer, CORS, Rate Limiting, startup admin seeder

**Key fixes during build**
- Npgsql 8.0.5 conflict → pinned Npgsql 8.0.6
- `Microsoft.AspNetCore.RateLimiting` package removed (built-in to .NET 8)
- ChatUseCase refactored to remove HttpContext dependency (Clean Architecture fix)
- `float[]` → `vector(768)` EF Core mapping: added value converter (`Pgvector.Vector`) + `ValueComparer<float[]>` in `MaterialChunkConfiguration`
- Added `IDesignTimeDbContextFactory<AppDbContext>` for migration tooling
- Admin startup seeder added to `Program.cs` (BCrypt work factor 11, email: admin@edurag.local, password: Admin@123)

**Frontend — React 18 + TypeScript + shadcn/ui**
- Vite 8 + React 19 + TypeScript 6, shadcn/ui New York style, Violet theme
- Tailwind CSS downgraded from v4 → v3 (project uses v3 config API)
- Student portal: playful violet theme, Nunito font, gradient grade cards, SSE chat with ReactMarkdown
- Admin portal: neutral professional UI, CRUD dialogs, react-dropzone upload, status badges
- JWT stored in sessionStorage (never localStorage)
- Axios interceptor for Bearer token, 401 → redirect to /login

**Database**
- PostgreSQL 18 on localhost:5433, database `Edurag`
- pgvector extension installed, HNSW-ready `MaterialChunks` table with `vector(768)` column
- EF Core `InitialCreate` migration applied — 8 tables created
- Storage directories created: `E:\EduRagFiles\EduRagPdfs`, `E:\EduRagFiles\Ollama`

**Configuration**
- `appsettings.json` / `appsettings.Development.json`: DB on port 5433, Ollama at localhost:11434, storage at `E:\EduRagFiles\`

### Docs updated
- [[../Architecture/04-Infrastructure-Layer]] — value converter pattern for float[] ↔ Pgvector.Vector, design-time factory
- [[../System/04-Configuration]] — confirmed storage paths, DB port 5433
- [[../Development/01-Setup-Guide]] — Tailwind v3 requirement, psql path on this machine

---

---

## 2026-06-19 — Vector Search Bug Fixes (Dimension Mismatch + Dapper Integration)

**Type:** Fix
**Affected:** Backend, Database

### What changed

**Fix 1 — `ChunkSearchResult` constructor order**
- Dapper maps record constructor parameters positionally against SQL column order
- SQL returns `ChunkId, Content, PageNumber, Score` but record had `Score` before `PageNumber`
- Fixed: `public record ChunkSearchResult(Guid ChunkId, string Content, int PageNumber, double Score)`

**Fix 2 — Shared `NpgsqlDataSource` for EF Core and Dapper**
- `new NpgsqlConnection(cs)` (raw, used by Dapper) does not inherit the `UseVector()` type mapping registered by EF Core's data source
- Npgsql resolved the `vector` type OID from the stale PostgreSQL catalog, producing a dimension mismatch at query time even though DB data and column type were both correct
- Fixed: `NpgsqlDataSourceBuilder` → `UseVector()` → `Build()` produces a singleton `NpgsqlDataSource`; both EF Core (`o.UseNpgsql(dataSource, ...)`) and Dapper (`dataSource.CreateConnection()`) use it

**Fix 3 — Native `Vector` object in `VectorSearchService`**
- Previously built a string literal `"[x1,x2,...]"` and cast with `@vector::vector` in SQL
- Now passes `new Vector(queryEmbedding)` directly; Npgsql uses the registered handler — no SQL cast needed
- Removed `::vector` from both SELECT expression and ORDER BY clause

**Fix 4 — `PendingMaterialsRequeueService` (new background service)**
- `Channel<Guid>` is in-memory; on API restart all queued IDs are lost
- Any material in `Pending` or `Failed` status after a restart would never be vectorized
- New `IHostedService` queries DB at startup and re-writes all such IDs to the channel
- Registered in `ServiceRegistration.cs` after `VectorizationWorker`

**Database fix (manual, run once on `Edurag` DB)**
```sql
TRUNCATE TABLE "MaterialChunks";
ALTER TABLE "MaterialChunks" ALTER COLUMN "Embedding" TYPE vector(768);
DELETE FROM "StudyMaterials";
```
Required when the `MaterialChunks` table was created outside EF Core migrations with `vector(1536)`.

### Docs updated
- [[../Architecture/04-Infrastructure-Layer]] — NpgsqlDataSource shared pattern, VectorSearchService native Vector usage, PendingMaterialsRequeueService
- [[../System/06-Troubleshooting]] — full dimension mismatch diagnosis + 3 root causes, pending-on-restart cause

---

## 2026-06-19 — Vector Search Locale Bug (Real Root Cause of Dimension Mismatch)

**Type:** Fix
**Affected:** Backend

### What changed

The persistent `expected 768 dimensions, not 1536` error was finally traced to a **culture/locale bug**, not a database or model issue.

- `VectorSearchService` built the query vector with `string.Join(",", queryEmbedding)`, which uses the current culture for float formatting
- On a comma-decimal locale, each float renders as `0,5` instead of `0.5`
- Since the vector literal is comma-delimited, Postgres parsed each decimal-comma as a separator → 768 floats became 1536 tokens (`768 × 2`)
- Diagnosis confirmed via diag endpoint: `embedding: ok (768 dims)` (C# array correct) yet SQL cast saw 1536
- Ollama `nomic-embed-text` verified at 768 dims; stored chunks verified at 768 — only the query literal was corrupt

**Fix**
```csharp
var vectorLiteral = "[" +
    string.Join(",", queryEmbedding.Select(f => f.ToString("R", CultureInfo.InvariantCulture))) +
    "]";
```

Storage was never affected because EF Core's `Pgvector.Vector` value converter serializes with invariant culture. Earlier hypotheses (Dapper data source mapping, `::vector` cast, DB column dimension) were contributing red herrings; the `NpgsqlDataSource` sharing and `::vector(768)` explicit cast were kept as defensive improvements.

### Docs updated
- [[../Architecture/04-Infrastructure-Layer]] — InvariantCulture requirement on the vector literal, with locale-bug callout
- [[../System/06-Troubleshooting]] — new top entry for the `expected 768, not 1536` locale bug
- [[../../CLAUDE.md]] — Common Mistakes row for culture-sensitive float formatting

---

*For the change tracking protocol, see [[../Skills/change-tracker]].*
