---
tags: [changelog, history]
created: 2026-06-18
updated: 2026-06-19 (3)
type: changelog
status: stable
aliases: [Changelog, Change History]
---

# Changelog

> [[_HOME|← Home]]

All changes to the EduRAG codebase and documentation are recorded here.

---

## 2026-06-19 — F-006 (amend): Student Portal PDF Preview

**Type:** Feature amendment
**Branch:** `feature-Display-pdf-chapter`
**Affected:** API (StudentController), Frontend (ClassSubjectSelectPage), Docs

Extended F-006 to the student portal. Students can now preview chapter PDFs directly from the chapter selection screen before starting a chat session.

- A violet **FileText** icon appears beside each chapter row (sibling button in a wrapper `<div>`) when `hasPdf: true`.
- Clicking opens a full-height Dialog PDF viewer styled in the student portal's violet theme — same blob URL / iframe pattern as the admin viewer.
- New backend endpoint `GET /api/student/chapters/{chapterId}/pdf` (Student-auth) in `StudentController` mirrors the admin endpoint but is role-scoped to students.
- Chapter card HTML restructured: was a single `<button>` (which would require invalid nested buttons); now a `<div>` wrapper with two sibling `<button>` elements — one for chapter selection, one for PDF preview.

### Files changed

| File | Change |
|------|--------|
| `EduRAG.API/Controllers/StudentController.cs` | Injects `IMaterialQueries` + `IFileStorageService`; new `GET /student/chapters/{chapterId}/pdf` action |
| `frontend/src/student/pages/ClassSubjectSelectPage.tsx` | Restructured chapter card to `div` wrapper; violet FileText icon button when `hasPdf`; blob-URL PDF viewer Dialog |
| `EduRagBrain/System/02-API-Reference.md` | Student PDF endpoint documented |
| `EduRagBrain/Architecture/06-Frontend.md` | `ClassSubjectSelectPage` component map entry updated |
| `EduRagBrain/Backlog/BACKLOG.md` | F-006 detail section extended with student portal info |
| `EduRagBrain/User/02-Student-Guide.md` | "Previewing a Chapter PDF" section added |

---

## 2026-06-19 — F-006: Display PDF for Chapter

**Type:** Feature
**Branch:** `feature-Display-pdf-chapter`
**Affected:** Application (DTOs, Interfaces), Infrastructure (Queries), API (AdminController), Frontend (ClassDetailPage), Docs

### What changed

Admins can now preview the PDF uploaded to any chapter directly from the **Class Detail** page, without navigating to the Materials list.

- A blue **FileText** icon appears on chapter rows that have an uploaded PDF (`HasPdf = true`). Chapters without uploads show nothing.
- Clicking the icon opens a full-height Dialog. The PDF is fetched as a blob via the authenticated `api` axios instance and rendered in an `<iframe>`. A **Download** button lets the admin save it locally. The blob URL is revoked on close to prevent memory leaks.
- New backend endpoint `GET /api/admin/chapters/{chapterId}/pdf` (Admin-auth) queries for the most recent material for the chapter and streams the file via `IFileStorageService.OpenRead`.
- `ChapterDto` gains a `HasPdf bool` field, computed in the existing Dapper query using an `EXISTS` subquery — no extra API call from the frontend.
- New `MaterialFileDto` DTO carries just `Id`, `StoredFilePath`, and `OriginalFileName` for file-serving purposes.

### Files changed

| File | Change |
|------|--------|
| `EduRAG.Application/DTOs/ChapterDtos.cs` | `ChapterDto` gains `HasPdf bool` |
| `EduRAG.Application/DTOs/MaterialDtos.cs` | New `MaterialFileDto` record |
| `EduRAG.Application/Interfaces/IQueryServices.cs` | `IMaterialQueries` gains `GetFileByChapterIdAsync` |
| `EduRAG.Infrastructure/Persistence/Queries/ChapterQueries.cs` | SQL adds `EXISTS` subquery for `HasPdf` |
| `EduRAG.Infrastructure/Persistence/Queries/MaterialQueries.cs` | Implements `GetFileByChapterIdAsync` |
| `EduRAG.API/Controllers/AdminController.cs` | Injects `IFileStorageService`; new `GET /admin/chapters/{chapterId}/pdf` |
| `frontend/src/types/index.ts` | `ChapterDto` gains `hasPdf: boolean` |
| `frontend/src/admin/pages/ClassDetailPage.tsx` | PDF icon on chapter rows; blob-URL PDF viewer Dialog |
| `EduRagBrain/Backlog/BACKLOG.md` | F-006 row + detail section |
| `EduRagBrain/System/02-API-Reference.md` | New endpoint documented |
| `EduRagBrain/Architecture/06-Frontend.md` | Component map and shadcn table updated |
| `EduRagBrain/User/01-Admin-Guide.md` | PDF preview usage documented |

---

## 2026-06-19 — B-006: Student Sees Empty Subject List After Admin Grants Permissions

**Type:** Fix
**Branch:** `feature-StudentSubjectPermission`
**Affected:** Infrastructure (Dapper query), Docs

### What changed

`SubjectQueries.GetByClassIdForStudentAsync` contained a correlated subquery that used an unqualified `"Id"` column reference. Both `"Subjects"` (outer) and `"StudentPermissions"` (inner) have an `"Id"` column. PostgreSQL resolves unqualified names from the innermost scope first, so `"Id"` resolved to `StudentPermissions."Id"` (type `uuid`) instead of `Subjects."Id"` (type `int`). The implicit `int = uuid` comparison always evaluated to false, making `EXISTS` return nothing when any permission rows existed — so students with assigned permissions saw an empty subject list.

The `NOT EXISTS` fallback (open access when no permissions exist) was correct, which is why students without any permissions could see all subjects while students with permissions could see nothing.

**Fix:** Added table alias `s` to `"Subjects"` and qualified all column references in the correlated subquery with `s."Id"`.

### Files changed

| File | Change |
|------|--------|
| `EduRAG.Infrastructure/Persistence/Queries/SubjectQueries.cs` | Add alias `s` to outer `"Subjects"`; use `s."Id"` in correlated EXISTS |
| `EduRagBrain/System/06-Troubleshooting.md` | New "Student Permission Issues" section with root cause, fix, and diagnosis SQL |
| `CLAUDE.md` | New row in "Common Mistakes" table — unaliased outer table in correlated subquery |
| `EduRagBrain/Backlog/BACKLOG.md` | B-006 row in Bug Fixes table; B-006 detail section |

---

## 2026-06-19 — F-006: Student Subject Permission Enforcement

**Type:** Feature (activation of existing backend design)
**Branch:** `feature-Vectorisation-based-on-class-subject-chapter`
**Affected:** Application (Interfaces), Infrastructure (Queries), API (StudentController), Frontend (ClassSubjectSelectPage), Docs

### What changed

The student subject permission feature was designed and fully implemented in the backend (entities, DTOs, migrations, repositories, use cases, admin controller endpoints) but was disconnected from the frontend because the student-facing subjects endpoint was missing the permission-aware fallback query.

**Root cause:** `ClassSubjectSelectPage.tsx` was calling `GET /student/my-subjects` which used `GetPermittedSubjectsAsync` — a query with no fallback for students with no permission rows. Students with no explicit grants saw an empty subject list instead of all subjects.

**Fixes applied:**

1. `ISubjectQueries` — added `GetByClassIdForStudentAsync(int classId, Guid studentId)`. Implements fallback: if student has no permission rows → all active subjects for the class; if any rows exist → only permitted subjects.

2. `SubjectQueries.cs` — implemented the method with a single SQL using `NOT EXISTS` / `EXISTS` subquery pattern (no N+1).

3. `StudentController.cs` — removed `GET /student/my-subjects`; added `GET /student/classes/{classId}/subjects` wired to `GetByClassIdForStudentAsync`.

4. `ClassSubjectSelectPage.tsx` — redesigned from 3-step (class → subject → chapter) to 2-step (subject → chapter). Auto-loads the student's class via `GET /student/my-class`, then fetches permission-filtered subjects via the new endpoint. Uses `SubjectDto` (fields `id`/`name`) instead of the old `StudentSubjectDto` (fields `subjectId`/`subjectName`).

### Permission behaviour

| Scenario | Student sees |
|----------|-------------|
| No permissions configured | All active subjects in their class |
| Permissions configured | Only the explicitly granted subjects |
| Student has no class assigned | "No class assigned yet" message, cannot start session |

### Files changed

| File | Change |
|------|--------|
| `EduRAG.Application/Interfaces/IQueryServices.cs` | Add `GetByClassIdForStudentAsync` to `ISubjectQueries` |
| `EduRAG.Infrastructure/Persistence/Queries/SubjectQueries.cs` | Implement `GetByClassIdForStudentAsync` with fallback SQL |
| `EduRAG.API/Controllers/StudentController.cs` | Replace `my-subjects` with `classes/{classId}/subjects` |
| `frontend/src/student/pages/ClassSubjectSelectPage.tsx` | 2-step flow; permission-filtered subjects endpoint |
| `EduRagBrain/System/02-API-Reference.md` | Document new `/student/classes/{classId}/subjects` endpoint |
| `EduRagBrain/User/01-Admin-Guide.md` | Document class requirement for student creation |

---

## 2026-06-19 — B-005: EF Core Vector Dimension Hardcode + Missing MistralAI Provider

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
