---
tags: [skill, claude, ai-assistant, rules]
created: 2026-06-18
updated: 2026-06-18
type: skill
status: stable
aliases: [Claude Skill, AI Rules, EduRAG Assistant Rules]
---

# EduRAG Claude Skill

> [[_HOME|← Home]]

This document defines how Claude behaves when working inside the EduRAG project.

---

## Identity

Claude is the development assistant for EduRAG — an AI-powered education platform built with ASP.NET Core 8, React 18, PostgreSQL + pgvector, and Ollama (llama3.2 + nomic-embed-text).

---

## Core Principles

1. **Architecture is law.** The Clean Architecture dependency rule is inviolable: Domain → Application → Infrastructure → API, arrows inward only. Never suggest importing Infrastructure into Application.

2. **EF Core for writes, Dapper for reads.** Never use EF Core for SELECT queries. Never use Dapper for INSERT/UPDATE/DELETE. This is the CQRS-lite pattern of this project.

3. **All AI is local.** Never suggest OpenAI, Anthropic, Azure OpenAI, or any paid API. All AI calls go through Ollama at `http://localhost:11434`. Embedding model: `nomic-embed-text` (768 dims). Chat model: `llama3.2`.

4. **Obsidian-first documentation.** All documentation lives in `EduRagBrain/`. Changes to code must be reflected immediately in the relevant docs. See [[change-tracker]].

5. **Security is non-negotiable.** Always enforce BCrypt passwords, JWT validation, role-based auth, parameterized SQL, and scoped vector search (classId + subjectId filters always present).

6. **shadcn/ui for student portal.** The student-facing UI uses shadcn/ui with a violet theme, Nunito/Inter fonts, rounded cards, and a playful but focused design for children and teenagers (ages 8–18).

---

## Change Protocol

When any code is changed, Claude must:

1. Check `EduRagBrain/` for all docs that reference the changed component
2. Update every affected doc immediately — do not defer
3. Add a changelog entry to [[../Changelog/CHANGELOG]]
4. If a new entity, endpoint, or config key is added, create or update the relevant doc section
5. If a component is renamed or removed, find all wikilinks to it and update them

---

## Folder Knowledge

| Folder | What it contains |
|--------|-----------------|
| `EduRagBrain/Architecture/` | Layer docs, entity models, API layer |
| `EduRagBrain/System/` | DB schema, API reference, security, config, deployment |
| `EduRagBrain/User/` | Getting started, admin guide, student guide, FAQ |
| `EduRagBrain/Development/` | Build order, setup, testing |
| `EduRagBrain/Skills/` | This file + change tracker |
| `EduRagBrain/Changelog/` | All changes to code and docs |
| `EduRagBrain/SRS/` | Original PDF specification (read-only reference) |
| `EduRagBrain/SRSmd/` | Markdown version of the spec (read-only reference) |

---

## Naming Conventions

| Context | Convention |
|---------|-----------|
| C# classes | PascalCase |
| C# methods/properties | PascalCase |
| C# private fields | `_camelCase` |
| TypeScript components | PascalCase |
| TypeScript hooks | `useXxx` |
| TypeScript files | `kebab-case.tsx` or `PascalCase.tsx` |
| Database tables | `"PascalCase"` (quoted PostgreSQL) |
| Database columns | `"PascalCase"` (quoted PostgreSQL) |
| API routes | `/api/kebab-case` |
| Obsidian docs | `00-kebab-case.md` (numbered prefix) |

---

## Code Quality Rules

- No comments unless the WHY is non-obvious
- No AutoMapper — manual mapping only
- No MediatR — use-cases called directly from controllers
- No `async void` — always `async Task`
- No bare `catch (Exception)` without logging
- All repository methods return `Task<T>` not `Task`
- Streaming endpoints must set `Content-Type: text/event-stream` and `Cache-Control: no-cache`

---

## Frontend Rules

- **Admin portal:** Professional, neutral — Tailwind defaults with grey accents
- **Student portal:** Playful, age-appropriate — violet accents, Nunito font, rounded-2xl cards, emoji subject icons, animated feedback
- All shadcn components before any custom components
- React Query for all server state (no raw `useEffect` for data fetching)
- JWT stored in memory only (never `localStorage`)
- Axios instance with interceptors — not raw `fetch` (except SSE streaming which uses native `fetch`)

---

## Common Pitfalls to Avoid

| Pitfall | Correct approach |
|---------|----------------|
| `Channel<Guid>` registered as Scoped | Must be Singleton |
| Forgetting `UseVector()` in DI | `o.UseNpgsql(cs, npg => npg.UseVector())` |
| Forgetting `mb.HasPostgresExtension("vector")` | Always in `OnModelCreating` |
| Using wrong embedding dimension | nomic-embed-text = 768, not 1536 |
| Missing `[EnumeratorCancellation]` on streaming | Required for proper cancellation |
| Buffered response on SSE endpoint | Must disable response buffering |
| Using `AllowAnyOrigin()` in production | Use `WithOrigins(specific_url)` |

---

## Related Docs

- [[change-tracker]] — detailed change tracking protocol
- [[../Architecture/00-Overview]] — system architecture
- [[../Development/00-Build-Order]] — build phases
