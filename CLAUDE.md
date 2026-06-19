# EduRAG — Project Instructions for Claude

## Project Identity

EduRAG is an AI-powered education platform.
- **Backend:** ASP.NET Core 8 (C# 12), Clean Architecture
- **Frontend:** React 18 + TypeScript + shadcn/ui (violet theme, children/teen design)
- **Database:** PostgreSQL 16 + pgvector (cosine similarity, HNSW index)
- **AI:** Ollama (local) — `nomic-embed-text` (768d embeddings) + `llama3.2` (chat)
- **Auth:** JWT Bearer, Admin and Student roles

## Knowledge Base

All documentation lives in `EduRagBrain/`. Open it as an Obsidian vault.

- Start at `EduRagBrain/_HOME.md` — the main map of content
- Architecture docs: `EduRagBrain/Architecture/`
- System docs (DB, API, security, config): `EduRagBrain/System/`
- User guides: `EduRagBrain/User/`
- Development docs (build order, setup, testing): `EduRagBrain/Development/`
- Claude rules: `EduRagBrain/Skills/edurag-claude-skill.md`

## MANDATORY: Before Any Code Change

1. Read `EduRagBrain/Skills/edurag-claude-skill.md` for full rules
2. Read `EduRagBrain/Skills/change-tracker.md` for the doc update protocol
3. After making changes, update every affected doc immediately
4. Add a changelog entry to `EduRagBrain/Changelog/CHANGELOG.md`

## Critical Architecture Rules

- **Dependency rule:** Domain ← Application ← Infrastructure ← API (inward only)
- **EF Core:** writes only (INSERT/UPDATE/DELETE)
- **Dapper:** reads only (SELECT)
- **AI:** all via Ollama at `http://localhost:11434` — NO paid APIs ever
- **Vector dimensions:** nomic-embed-text = 768. Do not change without updating schema.
- **Channel<Guid>:** must be registered as Singleton for VectorizationWorker

## Frontend Design

- **Student portal:** shadcn/ui, violet theme (`#7c3aed`), Nunito font, rounded-2xl, playful for ages 8–18
- **Admin portal:** professional, neutral, grey Tailwind defaults
- shadcn/ui `init` settings: New York style, Violet base colour, TypeScript
- JWT stored in memory only — never localStorage

## Security Non-Negotiables

- Passwords: BCrypt work factor 11
- SQL: parameterized only (Dapper @params + EF Core LINQ)
- CORS: `WithOrigins(specific)` only — never `AllowAnyOrigin()` in production
- Vector search: always filter by `ClassId + SubjectId`
- Session: always verify `ChatSession.UserId == authenticated user`

## Common Mistakes to Prevent

| Mistake | Correct |
|---------|---------|
| Channel<Guid> Scoped | Must be Singleton |
| Forgetting `npg.UseVector()` | Required in DI |
| Forgetting `mb.HasPostgresExtension("vector")` | Required in OnModelCreating |
| 1536-dim embedding | nomic-embed-text = 768 dims |
| EF Core for SELECT | Use Dapper |
| Raw fetch() for API calls | Use axios instance (except SSE streaming) |
| `new NpgsqlConnection(cs)` for Dapper | Use `dataSource.CreateConnection()` — EF Core and Dapper must share the same `NpgsqlDataSource` so `UseVector()` applies to all connections |
| `@vector::vector` untyped cast in Dapper SQL | Use `@vector::vector(768)` with explicit dimension — prevents silent mismatches and documents the expected size |
| `string.Join(",", floats)` for vector literal | Format with `CultureInfo.InvariantCulture` (`f.ToString("R", InvariantCulture)`) — comma-decimal locales render `0,5` and silently double 768 → 1536 dims |
| `ChunkSearchResult` wrong constructor order | Must match SQL column order (Dapper is positional): `(Guid ChunkId, string Content, int PageNumber, double Score)` |
| No `PendingMaterialsRequeueService` | Channel is in-memory; Pending/Failed materials are silently lost on API restart without a startup re-queue |

## File Layout

```
EduRag/
├── CLAUDE.md                  ← you are here
├── EduRagBrain/               ← Obsidian vault (all documentation)
│   ├── _HOME.md               ← start here
│   ├── Architecture/
│   ├── System/
│   ├── User/
│   ├── Development/
│   ├── Skills/
│   ├── Changelog/
│   ├── SRS/                   ← original PDF (read-only)
│   └── SRSmd/                 ← markdown spec (read-only)
├── src/                       ← .NET solution (to be created)
│   ├── EduRAG.Domain/
│   ├── EduRAG.Application/
│   ├── EduRAG.Infrastructure/
│   └── EduRAG.API/
└── frontend/                  ← React app (to be created)
```
