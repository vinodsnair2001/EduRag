---
tags: [backlog, features, bugfix, refactor, tracking]
created: 2026-06-19
updated: 2026-06-19
type: backlog
status: active
aliases: [Backlog, Feature Tracker, Task Log]
---

# EduRAG — Backlog & Task Tracker

> [[_HOME|← Home]]

This document is the single source of truth for all planned, in-progress, and completed work items.
Update this file whenever a task is started, completed, or a new requirement is identified.

---

## How to Use

| Column | Values |
|--------|--------|
| **Status** | `Planned` · `In Progress` · `Done` · `Cancelled` |
| **Type** | `Feature` · `Fix` · `Refactor` · `Docs` · `Security` |
| **Branch** | Git branch name (blank if not yet branched) |

Add new rows at the **top** of each section. Never delete rows — change status to `Cancelled` instead.

---

## Features

| # | Title | Status | Branch | Notes |
|---|-------|--------|--------|-------|
| F-005 | Chapter-based vectorisation & student chapter selection | Done | `feature-Vectorisation-based-on-class-subject-chapter` | RAG search scoped to Class+Subject+Chapters; student picks chapters before chat; admin selects chapter on upload |
| F-004 | Edit & deactivate student accounts | Done | `feature-StudentClassSubjectPermission` | Admin can update student profile/class/password; DELETE soft-deactivates (IsActive=false), data preserved |
| F-003 | Student class & subject permissions | Done | `feature-StudentClassSubjectPermission` | Admin assigns student to class + permitted subjects; student sees only their class and permitted subjects |
| F-002 | MistralAI provider support | Done | `feature-ImplementationMisteralAI` | Configurable AI backend (Ollama / MistralAI) via `AI:Provider` key |
| F-001 | Initial full-stack build | Done | `main` | Backend, Frontend, DB, RAG pipeline, SSE chat |

---

## Bug Fixes

| # | Title | Status | Branch | Notes |
|---|-------|--------|--------|-------|
| B-006 | Student sees empty subject list after admin grants permissions | Done | `feature-StudentSubjectPermission` | Correlated subquery in `GetByClassIdForStudentAsync` — unaliased `"Id"` resolved to `StudentPermissions."Id"` (Guid) not `Subjects."Id"` (int); type mismatch made EXISTS always false; fixed with table alias `s` |
| B-005 | EF Core vector dimension hardcode + missing MistralAI provider | Done | `feature-Vectorisation-based-on-class-subject-chapter` | `HasColumnType("vector(768)")` hardcoded in MaterialChunkConfig; AppDbContext now reads `AI:EmbeddingDimensions` from config; MistralAIService ported from main; ServiceRegistration conditionally picks provider |
| B-004 | Student portal calling removed `/student/classes` endpoints | Done | `feature-StudentClassSubjectPermission` | Frontend not updated when StudentController endpoints changed; rewired to `my-class` + `my-subjects` |
| B-003 | Vector locale bug — float comma-decimal → 1536 dims | Done | `main` | `string.Join` used current culture; fixed with `InvariantCulture` |
| B-002 | Dapper + EF Core share `NpgsqlDataSource` (UseVector) | Done | `main` | Raw `NpgsqlConnection` missed `vector` OID registration |
| B-001 | `ChunkSearchResult` constructor positional mismatch | Done | `main` | Score/PageNumber swapped; Dapper is positional not named |

---

## Refactors

| # | Title | Status | Branch | Notes |
|---|-------|--------|--------|-------|
| R-001 | `PendingMaterialsRequeueService` — re-queue on restart | Done | `main` | Channel is in-memory; Pending/Failed materials lost on restart |

---

## Planned / Upcoming

> Move rows from here to the correct section above once they are branched.

| # | Title | Type | Priority | Notes |
|---|-------|------|----------|-------|
| P-003 | Student chat session scoped to permitted subjects only | Fix | Medium | ChatUseCase should validate student has permission for the subject |
| P-004 | Practice questions feature | Feature | Medium | Per-subject quiz generation via llama3.2 |

---

## Feature F-005 Detail — Chapter-Based Vectorisation & Student Chapter Selection

**Branch:** `feature-Vectorisation-based-on-class-subject-chapter`
**Status:** Done (2026-06-19)

### Requirement

RAG search is currently scoped to Class + Subject. This feature adds Chapter-level scoping:

1. **Admin upload**: When uploading a PDF, admin can optionally select a Chapter. The PDF's chunks are stored with that `ChapterId` on `MaterialChunks`.
2. **Student session**: When starting a chat/quiz/summary session, the student:
   - Sees their permitted subjects (from `GET /student/my-subjects`)
   - Selects a subject
   - Sees checkboxes for all chapters in that subject
   - Selects one or more chapters (or all)
   - The chat session stores the selected chapter IDs
3. **RAG search**: Vector search filters by `ClassId + SubjectId + ChapterIds[]`. If multiple chapters are selected, chunks from all selected chapters are eligible. Materials without a chapter (legacy/subject-level uploads) are excluded from chapter-scoped sessions.

### Design Decisions

- `ChatSessions` gets a new `SelectedChapterIds TEXT` column storing a JSON int array `[1,2,3]`. Empty array = subject-level (no chapter filter applied, queries all chunks for the subject).
- `VectorSearchService.SearchAsync` adds an optional `int[]? chapterIds` parameter. When non-empty, adds `AND "ChapterId" = ANY(@chapterIds)` to the WHERE clause.
- The existing `MaterialChunks.ChapterId` (nullable INT) is already in the schema — no structural DB changes needed beyond `ChatSessions`.
- A composite index `(ClassId, SubjectId, ChapterId)` on `MaterialChunks` is added for efficient filtered search.
- Uploading without a chapter produces subject-level chunks (`ChapterId = NULL`). Subject-level chunks are included when `chapterIds` is empty but excluded when specific chapters are selected — this gives "start a chapter-scoped session" clean isolation.
- `ClassSubjectSelectPage` uses permission-scoped endpoints (`GET /student/my-class`, `GET /student/my-subjects`) consistent with F-003.

### Task Checklist

#### Backend

- [x] `ChatSession.cs` — add `SelectedChapterIds string?` property
- [x] `ChatDtos.cs` — update `CreateSessionRequest` to include `ChapterIds int[]`
- [x] `IVectorSearchService` — add `int[]? chapterIds` param to `SearchAsync`
- [x] `VectorSearchService.cs` — update SQL WHERE with chapter filter + add composite index note
- [x] `ChatUseCase.cs` — pass `ChapterIds` to `CreateSessionAsync`; pass from session to `SearchAsync`
- [x] `ChatController.cs` — forward `ChapterIds` from request to use case; update diag endpoint
- [x] `ChatRepository.cs` — persist `SelectedChapterIds` on session create; load it on `GetSessionAsync`
- [x] EF Migration — `AddChapterIdsToChatSessions`

#### Frontend

- [x] `MaterialListPage.tsx` — add chapter dropdown (fetches chapters when subject selected; optional)
- [x] `ClassSubjectSelectPage.tsx` — two-step UI: subject cards (permission-scoped) → chapter checkboxes → Start
- [x] `ChatPage.tsx` — read `chapterIds` from location state; send to `POST /chat/sessions`; show selected chapters in header
- [x] `types/index.ts` — `ChapterDto` already present; no changes needed

#### Docs

- [x] `EduRagBrain/System/01-Database-Schema.md` — update `ChatSessions` table; new index
- [x] `EduRagBrain/System/02-API-Reference.md` — update `/chat/sessions` + `/admin/upload`
- [x] `EduRagBrain/Architecture/07-AI-Pipeline.md` — update vector search description
- [x] `EduRagBrain/Architecture/02-Domain-Layer.md` — update `ChatSession` entity
- [x] `EduRagBrain/Architecture/06-Frontend.md` — update student portal flow
- [x] `EduRagBrain/User/01-Admin-Guide.md` — document chapter selection on upload
- [x] `EduRagBrain/Changelog/CHANGELOG.md` — add changelog entry

### New / Changed Endpoints

| Method | Path | Change |
|--------|------|--------|
| `POST /chat/sessions` | Add `chapterIds: int[]` to request body |
| `POST /admin/upload` | `chapterId` field already present (now surfaced in UI) |

### Files Changed

**Domain**
- `EduRAG.Domain/Entities/ChatSession.cs` — added `SelectedChapterIds`

**Application**
- `EduRAG.Application/DTOs/ChatDtos.cs` — updated `CreateSessionRequest`
- `EduRAG.Application/Interfaces/IQueryServices.cs` — updated `IVectorSearchService`
- `EduRAG.Application/UseCases/ChatUseCase.cs` — chapter IDs flow end-to-end

**Infrastructure**
- `EduRAG.Infrastructure/Services/VectorSearchService.cs` — chapter filter SQL
- `EduRAG.Infrastructure/Persistence/Repositories/ChatRepository.cs` — store/load `SelectedChapterIds`
- `EduRAG.Infrastructure/Migrations/20260619204900_AddChapterIdsToChatSessions.cs`
- `EduRAG.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` — updated

**API**
- `EduRAG.API/Controllers/ChatController.cs` — forward chapter IDs

**Frontend**
- `frontend/src/admin/pages/MaterialListPage.tsx` — chapter dropdown
- `frontend/src/student/pages/ClassSubjectSelectPage.tsx` — chapter selection step (permission-scoped)
- `frontend/src/student/pages/ChatPage.tsx` — chapter IDs in session creation + header

---

## Bug B-006 Detail — Student Sees Empty Subject List After Admin Grants Permissions

**Branch:** `feature-StudentSubjectPermission`
**Status:** Done (2026-06-19)

### Symptom

Admin saves subject permissions for a student via `PUT /admin/students/{id}/permissions`. Permissions are stored correctly in the `StudentPermissions` table. But when the student logs in, `ClassSubjectSelectPage` shows "No subjects assigned yet. Ask your teacher!" — the API returns an empty array.

### Root Cause

`SubjectQueries.GetByClassIdForStudentAsync` contained a correlated subquery without a table alias on the outer `"Subjects"` table:

```sql
-- BROKEN: "Id" resolves to StudentPermissions."Id" (Guid), not Subjects."Id" (int)
OR EXISTS (SELECT 1 FROM "StudentPermissions"
           WHERE "StudentId" = @studentId AND "SubjectId" = "Id")
```

Both `"Subjects"` and `"StudentPermissions"` have a column named `"Id"`. PostgreSQL resolves unqualified column references from the innermost scope outward. Inside the `EXISTS` subquery, `"Id"` matched `StudentPermissions."Id"` (type `uuid`) instead of the intended `Subjects."Id"` (type `int`). The implicit `int = uuid` comparison always evaluated to false, so no subject rows were ever returned once any permission records existed.

The `NOT EXISTS` half of the condition (open-access fallback) was correct — so students with zero permissions saw all subjects, but students with any permissions saw nothing.

### Fix

Added table alias `s` to the outer `"Subjects"` table and qualified all column references in the correlated subquery:

```sql
-- FIXED: s."Id" unambiguously refers to Subjects."Id" (int)
SELECT s."Id", s."Name", s."Description", s."ClassId", s."IsActive"
FROM "Subjects" s
WHERE s."ClassId" = @classId AND s."IsActive" = TRUE
  AND (
    NOT EXISTS (SELECT 1 FROM "StudentPermissions" WHERE "StudentId" = @studentId)
    OR EXISTS  (SELECT 1 FROM "StudentPermissions" WHERE "StudentId" = @studentId AND "SubjectId" = s."Id")
  )
ORDER BY s."Name"
```

### Files Changed

| File | Change |
|------|--------|
| `EduRAG.Infrastructure/Persistence/Queries/SubjectQueries.cs` | Add alias `s` to `"Subjects"`; qualify all column refs in correlated subquery |
| `EduRagBrain/System/06-Troubleshooting.md` | New "Student Permission Issues" section with diagnosis query |
| `CLAUDE.md` | New row in "Common Mistakes" — unaliased outer table in correlated subquery |
| `EduRagBrain/Changelog/CHANGELOG.md` | B-006 entry |

### Prevention

Any Dapper SQL that uses a correlated subquery where the inner table shares a column name with the outer table **must** alias the outer table and qualify the column reference. Added to `CLAUDE.md` common mistakes table.

---

## Feature F-004 Detail — Edit & Deactivate Student Accounts

**Branch:** `feature-StudentClassSubjectPermission`
**Status:** Done (2026-06-19)

### Requirement

Admins need to maintain student accounts after creation:
- **Edit**: update full name, email, assigned class, active status, and optionally reset password.
- **Deactivate**: suspend a student without losing their data; they are blocked from logging in immediately.

### Design Decisions

- Edit uses `PUT /api/admin/students/{id}` with a full-replacement body (`UpdateStudentRequest`). `NewPassword` is optional — omitting it preserves the existing hash.
- Email uniqueness is re-validated on edit; a conflict against a *different* account returns 400.
- `DELETE /api/admin/students/{id}` is a **soft delete** — sets `IsActive = false`. All data (chat history, permissions, class assignment) is preserved. The student is blocked from login because `AuthUseCase` already guards on `!user.IsActive`.
- Reactivation is done via `PUT /admin/students/{id}` with `isActive: true` — no separate endpoint needed.
- Returns 400 (not 404) when trying to deactivate an already-deactivated student, to distinguish "not found" from "no-op".

### New Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `PUT` | `/api/admin/students/{id}` | Admin | Update student profile, class, active status, optional password reset |
| `DELETE` | `/api/admin/students/{id}` | Admin | Soft-deactivate student (IsActive=false); data preserved; login blocked |

### Files Changed

**Application**
- `EduRAG.Application/DTOs/UserDtos.cs` — new `UpdateStudentRequest`
- `EduRAG.Application/Interfaces/IRepositories.cs` — added `UpdateAsync` and `DeleteAsync` to `IUserRepository`
- `EduRAG.Application/UseCases/ManageStudentUseCase.cs` — new `UpdateStudentAsync` and `DeleteStudentAsync`

**Infrastructure**
- `EduRAG.Infrastructure/Persistence/Repositories/UserRepository.cs` — implemented `UpdateAsync` and `DeleteAsync`

**API**
- `EduRAG.API/Controllers/AdminController.cs` — `PUT /admin/students/{id}` and `DELETE /admin/students/{id}`

---

## Feature F-003 Detail — Student Class & Subject Permissions

**Branch:** `feature-StudentClassSubjectPermission`
**Status:** Done (2026-06-19)

### Requirement

When a student is created by an admin:
1. The admin assigns the student to a **class** (one class per student).
2. The admin grants the student permission for specific **subjects** within that class.
3. When the student logs in, they see only **their assigned class** and only the **subjects they have permission for**.

### Design Decisions

- `AppUser.ClassId` (nullable `INT FK → Classes`) — null for Admin users, required for Students.
- New `StudentPermissions` join table `(StudentId FK → AppUsers, SubjectId FK → Subjects)` with a unique constraint on `(StudentId, SubjectId)`.
- Setting permissions replaces the full set (DELETE + INSERT) — no partial update endpoint needed.
- Student endpoints `GET /student/my-class` and `GET /student/my-subjects` replace the old open endpoints (`/student/classes` and `/student/classes/{id}/subjects` which returned all data without scoping).
- Admin creates students via `POST /admin/students` (distinct from generic `POST /auth/register`) to enforce class assignment at creation time.

### New Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `POST` | `/api/admin/students` | Admin | Create student with assigned class |
| `GET` | `/api/admin/students/{id}/permissions` | Admin | List subject permissions for a student |
| `PUT` | `/api/admin/students/{id}/permissions` | Admin | Replace all subject permissions for a student |
| `GET` | `/api/student/my-class` | Student | Get the student's assigned class |
| `GET` | `/api/student/my-subjects` | Student | Get the student's permitted subjects |

### Files Changed

**Domain**
- `EduRAG.Domain/Entities/StudentPermission.cs` — new entity
- `EduRAG.Domain/Entities/AppUser.cs` — added `ClassId`, `Class`, `SubjectPermissions`
- `EduRAG.Domain/Entities/Subject.cs` — added `StudentPermissions` navigation

**Application**
- `EduRAG.Application/DTOs/UserDtos.cs` — `UserDto` adds `ClassId`; new `CreateStudentRequest`, `SetStudentPermissionsRequest`, `StudentPermissionDto`, `StudentClassDto`, `StudentSubjectDto`
- `EduRAG.Application/Interfaces/IRepositories.cs` — new `IStudentPermissionRepository`
- `EduRAG.Application/Interfaces/IQueryServices.cs` — new `IStudentPermissionQueries`
- `EduRAG.Application/UseCases/ManageStudentUseCase.cs` — new use case

**Infrastructure**
- `EduRAG.Infrastructure/Persistence/Configurations/StudentPermissionConfiguration.cs` — new EF config
- `EduRAG.Infrastructure/Persistence/Configurations/AppUserConfiguration.cs` — added `ClassId` FK
- `EduRAG.Infrastructure/Persistence/AppDbContext.cs` — added `StudentPermissions` DbSet
- `EduRAG.Infrastructure/Persistence/Repositories/StudentPermissionRepository.cs` — new EF write repo
- `EduRAG.Infrastructure/Persistence/Queries/StudentPermissionQueries.cs` — new Dapper read queries
- `EduRAG.Infrastructure/Persistence/Queries/UserQueries.cs` — added `ClassId` to SELECT
- `EduRAG.Infrastructure/ServiceRegistration.cs` — registered new services
- `EduRAG.Infrastructure/Migrations/20260619112422_AddStudentClassPermissions.cs` — new migration

**API**
- `EduRAG.API/Controllers/AdminController.cs` — 3 new student/permission endpoints
- `EduRAG.API/Controllers/StudentController.cs` — replaced with `my-class`, `my-subjects`, chapters

---

*See [[../Changelog/CHANGELOG]] for the full history.*
