---
tags: [changelog, history]
created: 2026-06-18
updated: 2026-06-19 (2)
type: changelog
status: stable
aliases: [Changelog, Change History]
---

# Changelog

> [[_HOME|← Home]]

All changes to the EduRAG codebase and documentation are recorded here.

---

## 2026-06-19 — Admin: subject permission editing on student edit dialog

**Type:** Feature
**Branch:** `feature-StudentClassSubjectPermission`
**Affected:** Frontend

### What changed

- **`types/index.ts`** — added `StudentPermissionDto` interface (`id`, `studentId`, `subjectId`, `subjectName`, `grantedAt`)
- **`UserManagementPage.tsx`** — edit student dialog now includes a **Subject Access** section:
  - Fetches class subjects via `GET /admin/classes/{classId}/subjects` (enabled when class is selected)
  - Fetches existing permissions via `GET /admin/students/{id}/permissions` (enabled when dialog opens)
  - Pre-checks subjects the student currently has access to
  - Clearing/changing the class resets subject selections
  - **Save Changes** calls both `PUT /admin/students/{id}` (profile) and `PUT /admin/students/{id}/permissions` (full-replace permissions) in sequence
  - Dialog content is scrollable (`max-h-[90vh] overflow-y-auto`) to handle long subject lists

### Docs updated
- [[../Architecture/06-Frontend]] — `UserManagementPage` component map entry updated
- [[../User/01-Admin-Guide]] — added "Edit a Student's Subject Permissions" section

---

## 2026-06-19 — Student Portal: wire class/subject to permission endpoints (feature-StudentClassSubjectPermission)

**Type:** Fix
**Branch:** `feature-StudentClassSubjectPermission`
**Affected:** Frontend

### What changed

`ClassSubjectSelectPage` was calling the old open endpoints (`GET /student/classes` and `GET /student/classes/{classId}/subjects`) which no longer exist on `StudentController`. It now calls the permission-scoped endpoints added in F-003.

#### `ClassSubjectSelectPage.tsx` — full rewrite
- Was: two-step flow — pick from a grid of **all** classes, then pick from **all** subjects for that class
- Now: single screen — loads the student's **one assigned class** via `GET /student/my-class`, then loads only their **permitted subjects** via `GET /student/my-subjects`
- Empty states for "no class assigned" and "no subjects assigned" guide the student to contact their teacher
- Subject cards still use emoji icons and grade-based gradient colours

#### `UserManagementPage.tsx` — admin user list
- Student rows now show a **pencil** (edit) icon and a **UserX / UserCheck** (deactivate/reactivate) icon
- Create dialog routes student accounts through `POST /admin/students` (with class dropdown) and admin accounts through `POST /auth/register`
- Both create and edit dialogs fetch `GET /admin/classes` for the class picker

#### `types/index.ts`
- Added `StudentClassDto { classId, className, grade }`
- Added `StudentSubjectDto { subjectId, subjectName, description }`
- Added `classId?: number` to `UserDto`

### Root cause
`StudentController` was rewritten to drop the old open `/student/classes` and `/student/classes/{classId}/subjects` routes in favour of the permission-scoped `my-class` / `my-subjects` routes, but the frontend was not updated at the same time.

### Docs updated
- [[../Architecture/06-Frontend]] — updated `ClassSubjectSelectPage` and `UserManagementPage` descriptions in component map

---

## 2026-06-19 — Edit & Deactivate Student Accounts (feature-StudentClassSubjectPermission)

**Type:** Feature
**Branch:** `feature-StudentClassSubjectPermission`
**Affected:** Backend, Docs

### What changed

Admins can now edit and deactivate student accounts via the API.

#### New DTO
- `UpdateStudentRequest` — `FullName`, `Email`, `ClassId`, `IsActive`, `NewPassword?`

#### Updated interfaces / implementations
- `IUserRepository` — added `UpdateAsync(AppUser)` and `DeleteAsync(Guid)` (DeleteAsync retained for future use)
- `UserRepository` — implemented both methods
- `ManageStudentUseCase` — new `UpdateStudentAsync` (validates email uniqueness, optional password reset) and `DeactivateStudentAsync` (soft delete — sets `IsActive = false`, data preserved)

#### New admin endpoints
| Method | Path | Description |
|--------|------|-------------|
| `PUT` | `/api/admin/students/{id}` | Edit student profile, class, active status, optional password reset |
| `DELETE` | `/api/admin/students/{id}` | Soft-deactivate (IsActive=false); student cannot log in; all data preserved |

#### Reactivation
Use `PUT /admin/students/{id}` with `"isActive": true` — no separate endpoint needed.

### Docs updated
- [[../System/02-API-Reference]] — added PUT and DELETE student endpoints with soft-delete semantics
- [[../Backlog/BACKLOG]] — added F-004 with detail section

---

## 2026-06-19 — Student Class & Subject Permissions (feature-StudentClassSubjectPermission)

**Type:** Feature
**Branch:** `feature-StudentClassSubjectPermission`
**Affected:** Backend, Database, Docs

### What changed

Added class assignment and subject-level access control for student accounts.

#### New entities / tables
- `StudentPermission` domain entity + `StudentPermissions` DB table — join table storing which subjects a student may access, with a unique constraint on `(StudentId, SubjectId)`
- `AppUser.ClassId` — nullable FK to `Classes`; `null` for Admin users, required for Student users

#### New files
- `EduRAG.Domain/Entities/StudentPermission.cs`
- `EduRAG.Application/UseCases/ManageStudentUseCase.cs`
- `EduRAG.Application/Interfaces/IRepositories.cs` — `IStudentPermissionRepository`
- `EduRAG.Application/Interfaces/IQueryServices.cs` — `IStudentPermissionQueries`
- `EduRAG.Infrastructure/Persistence/Configurations/StudentPermissionConfiguration.cs`
- `EduRAG.Infrastructure/Persistence/Repositories/StudentPermissionRepository.cs`
- `EduRAG.Infrastructure/Persistence/Queries/StudentPermissionQueries.cs`
- `EduRAG.Infrastructure/Migrations/20260619112422_AddStudentClassPermissions.cs`
- `EduRagBrain/Backlog/BACKLOG.md` — new task tracker document

#### Modified files
- `AppUser.cs` — added `ClassId`, `Class`, `SubjectPermissions`
- `Subject.cs` — added `StudentPermissions` navigation
- `AppUserConfiguration.cs` — added `ClassId` FK config
- `AppDbContext.cs` — added `StudentPermissions` DbSet
- `UserDtos.cs` — `UserDto` adds `ClassId`; 5 new DTO types
- `UserQueries.cs` — added `ClassId` to SELECT
- `ServiceRegistration.cs` — registered new use case, repository, and query service
- `AdminController.cs` — 3 new endpoints (create student, get/set permissions)
- `StudentController.cs` — replaced with `my-class`, `my-subjects`, chapters (dropped open class/subject list)

#### New admin endpoints
| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/admin/students` | Create student with class assignment |
| `GET` | `/api/admin/students/{id}/permissions` | List student's subject permissions |
| `PUT` | `/api/admin/students/{id}/permissions` | Replace all subject permissions |

#### New student endpoints
| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/student/my-class` | Student's assigned class |
| `GET` | `/api/student/my-subjects` | Student's permitted subjects only |

### Docs updated
- [[../Architecture/02-Domain-Layer]] — added `StudentPermission` entity, updated `AppUser`, updated entity hierarchy
- [[../System/01-Database-Schema]] — added `ClassId` to `AppUsers` DDL, new `StudentPermissions` DDL, updated ERD
- [[../System/02-API-Reference]] — added all 5 new endpoints
- [[../_HOME]] — added Backlog navigation section
- [[../Backlog/BACKLOG]] — created with F-003 detail, planned items P-001..P-004

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
