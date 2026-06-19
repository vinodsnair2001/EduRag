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
| F-002 | MistralAI provider support | Done | `feature-ImplementationMisteralAI` | Configurable AI backend (Ollama / MistralAI) via `AI:Provider` key |
| F-001 | Initial full-stack build | Done | `main` | Backend, Frontend, DB, RAG pipeline, SSE chat |

---

## Bug Fixes

| # | Title | Status | Branch | Notes |
|---|-------|--------|--------|-------|
| B-004 | EF Core vector dimension hardcode + missing MistralAI provider | Done | `feature-Vectorisation-based-on-class-subject-chapter` | `HasColumnType("vector(768)")` hardcoded in MaterialChunkConfig; AppDbContext now reads `AI:EmbeddingDimensions` from config; MistralAIService ported from main; ServiceRegistration conditionally picks provider |
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

| # | Title | Type | Priority | Notes |
|---|-------|------|----------|-------|
| P-001 | Student portal — class/subject/chapter selection UI | Done | High | Part of F-005 |
| P-002 | Admin upload — chapter selector | Done | High | Part of F-005 |
| P-003 | Vector search — chapter ID array filter | Done | High | Part of F-005 |
| P-004 | ChatSession — persist selected chapter IDs | Done | High | Part of F-005 |

---

## Feature F-005 Detail — Chapter-Based Vectorisation & Student Chapter Selection

**Branch:** `feature-Vectorisation-based-on-class-subject-chapter`
**Status:** Done (2026-06-19)

### Requirement

RAG search is currently scoped to Class + Subject. This feature adds Chapter-level scoping:

1. **Admin upload**: When uploading a PDF, admin can optionally select a Chapter. The PDF's chunks are stored with that `ChapterId` on `MaterialChunks`.
2. **Student session**: When starting a chat/quiz/summary session, the student:
   - Sees their available subjects
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
- [x] `ClassSubjectSelectPage.tsx` — two-step UI: subject cards → chapter checkboxes → Start
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
- `EduRAG.Infrastructure/Migrations/YYYYMMDDHHMMSS_AddChapterIdsToChatSessions.cs`
- `EduRAG.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` — updated

**API**
- `EduRAG.API/Controllers/ChatController.cs` — forward chapter IDs

**Frontend**
- `frontend/src/admin/pages/MaterialListPage.tsx` — chapter dropdown
- `frontend/src/student/pages/ClassSubjectSelectPage.tsx` — chapter selection step
- `frontend/src/student/pages/ChatPage.tsx` — chapter IDs in session creation + header

---

*See [[../Changelog/CHANGELOG]] for the full history.*
