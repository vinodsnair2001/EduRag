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

## 2026-06-19 — MistralAI Provider Support (feature-ImplementationMisteralAI)

**Type:** Feature
**Branch:** `feature-ImplementationMisteralAI`
**Affected:** Infrastructure, Configuration, Scripts, Docs

### What changed

Added MistralAI as an alternative AI provider alongside Ollama. The active provider is selected by a single config key (`AI:Provider`) — all embedding, vectorization, and chat functionality routes through the same `IAIService` interface.

#### New files
- `src/EduRAG.Infrastructure/Services/MistralAIService.cs` — implements `IAIService` using the MistralAI REST API (`mistral-embed` for embeddings, `mistral-large-latest` for streaming chat)
- `scripts/migrate-to-mistralai.sql` — SQL script to change `Embedding` column from `vector(768)` → `vector(1024)`, clear old chunks, reset materials to Pending
- `scripts/migrate-to-ollama.sql` — SQL script to revert from `vector(1024)` → `vector(768)`

#### Modified files
- `src/EduRAG.API/appsettings.json` — added `AI` config section (`Provider`, `EmbeddingDimensions`, `MistralAI.*`)
- `src/EduRAG.Infrastructure/ServiceRegistration.cs` — conditional `IAIService` registration based on `AI:Provider`
- `src/EduRAG.Infrastructure/Services/VectorSearchService.cs` — reads vector dimension from `AI:EmbeddingDimensions` config instead of hard-coding 768
- `src/EduRAG.Infrastructure/Persistence/AppDbContextFactory.cs` — now reads connection string from `appsettings.json` instead of hard-coding credentials

### Provider details

| | Ollama (default) | MistralAI |
|-|-----------------|-----------|
| Config | `AI:Provider = "Ollama"` | `AI:Provider = "MistralAI"` |
| Embed dims | 768 | 1024 |
| API key | Not required | `AI:MistralAI:ApiKey` |
| DB column | `vector(768)` | `vector(1024)` |

### Migration note

Switching providers requires running a SQL script (`scripts/migrate-to-*.sql`) to change the embedding column dimension and re-vectorizing all PDFs. Ollama config is unchanged — existing Ollama setups require no action.

### Docs updated
- `EduRagBrain/Architecture/07-AI-Pipeline.md` — updated with MistralAI data flow, service code, provider comparison table
- `EduRagBrain/System/04-Configuration.md` — added `AI` section, switching guide, env-var examples
- `EduRagBrain/Development/01-Setup-Guide.md` — added "Switching AI Provider" section with step-by-step instructions

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
