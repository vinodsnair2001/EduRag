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
| F-004 | Edit & deactivate student accounts | Done | `feature-StudentClassSubjectPermission` | Admin can update student profile/class/password; DELETE soft-deactivates (IsActive=false), data preserved |
| F-003 | Student class & subject permissions | Done | `feature-StudentClassSubjectPermission` | Admin assigns student to class + permitted subjects; student sees only their class and permitted subjects |
| F-002 | MistralAI provider support | Done | `feature-ImplementationMisteralAI` | Configurable AI backend (Ollama / MistralAI) via `AI:Provider` key |
| F-001 | Initial full-stack build | Done | `main` | Backend, Frontend, DB, RAG pipeline, SSE chat |

---

## Bug Fixes

| # | Title | Status | Branch | Notes |
|---|-------|--------|--------|-------|
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
| P-001 | Student portal — class/subject selection UI | Done | — | Rewired to `GET /student/my-class` + `GET /student/my-subjects`; single-screen layout |
| P-002 | Admin portal — student management page | Done | — | Edit/deactivate/reactivate per student row; create dialog with class picker |
| P-003 | Student chat session scoped to permitted subjects only | Fix | Medium | ChatUseCase should validate student has permission for the subject |
| P-004 | Practice questions feature | Feature | Medium | Per-subject quiz generation via llama3.2 |

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
