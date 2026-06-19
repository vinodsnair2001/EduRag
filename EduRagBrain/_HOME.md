---
tags: [moc, home, edurag]
created: 2026-06-18
updated: 2026-06-18
type: moc
status: stable
aliases: [Home, Index, EduRAG Brain]
---

# EduRAG Brain — Knowledge Base

> AI-Powered Education Platform · Full-Stack · Fully Local AI (Ollama)

```
Backend: ASP.NET Core 8 (C# 12)    Frontend: React 18 + TypeScript
Database: PostgreSQL 16 + pgvector  AI: Ollama (llama3.2 + nomic-embed-text)
Architecture: Clean Architecture (DDD)   Auth: JWT (Admin / Student roles)
```

---

## Navigation

### [[Architecture/00-Overview|Architecture]]
> How the system is designed — layers, patterns, decisions.

| Doc | Summary |
|-----|---------|
| [[Architecture/00-Overview]] | Bird's-eye view of all layers and how they connect |
| [[Architecture/01-Clean-Architecture]] | Layer rules, dependency direction, project layout |
| [[Architecture/02-Domain-Layer]] | Entities, enums, domain events — zero dependencies |
| [[Architecture/03-Application-Layer]] | Use-cases, interfaces (ports), DTOs |
| [[Architecture/04-Infrastructure-Layer]] | EF Core repos, Dapper queries, file storage |
| [[Architecture/05-API-Layer]] | Controllers, middleware, DI wiring |
| [[Architecture/06-Frontend]] | React app structure, routing, auth, streaming SSE |
| [[Architecture/07-AI-Pipeline]] | Ollama integration, RAG pipeline, vectorization worker |

### [[System/00-System-Overview|System]]
> Operational details — DB schema, endpoints, config, security.

| Doc | Summary |
|-----|---------|
| [[System/00-System-Overview]] | Component map, data flow, integration points |
| [[System/01-Database-Schema]] | Full PostgreSQL DDL, indexes, pgvector setup |
| [[System/02-API-Reference]] | All 22 endpoints — method, path, auth, body |
| [[System/03-Security]] | Auth, hashing, CORS, input validation, scope isolation |
| [[System/04-Configuration]] | appsettings.json, environment variables, docker-compose |
| [[System/05-Deployment]] | Docker, Ollama model pull, migration steps |
| [[System/06-Troubleshooting]] | Common errors and fixes |

### [[User/00-Getting-Started|User Guides]]
> How to use EduRAG — admin and student perspectives.

| Doc | Summary |
|-----|---------|
| [[User/00-Getting-Started]] | First-run checklist, login, roles explained |
| [[User/01-Admin-Guide]] | Classes, subjects, chapters, PDF upload, user management |
| [[User/02-Student-Guide]] | Class selection, chat, practice questions, answer submission |
| [[User/03-FAQ]] | Frequently asked questions |

### [[Development/00-Build-Order|Development]]
> How to build the project phase-by-phase.

| Doc | Summary |
|-----|---------|
| [[Development/00-Build-Order]] | 40-step phased build guide |
| [[Development/01-Setup-Guide]] | Prerequisites, toolchain, local environment |
| [[Development/02-Testing]] | Postman flows, integration test scenarios |

### [[Skills/edurag-claude-skill|Claude Skills]]
> Instructions and rules Claude follows when working in this project.

| Doc | Summary |
|-----|---------|
| [[Skills/edurag-claude-skill]] | Project-specific Claude behaviour rules |
| [[Skills/change-tracker]] | Protocol for keeping docs in sync with code changes |

---

## Key Concepts

- **RAG** — Retrieval-Augmented Generation: student questions → embed → cosine search → top-5 chunks → llama3.2 prompt
- **pgvector** — PostgreSQL extension storing 768-dim nomic-embed-text vectors with HNSW index
- **CQRS-lite** — EF Core for writes, Dapper for reads (no full CQRS bus)
- **SSE** — Server-Sent Events stream chat tokens from backend to browser
- **VectorizationWorker** — `BackgroundService` + `Channel<Guid>` queue; processes PDFs after upload

## Data Flow (one-liner)

```
Admin uploads PDF → SHA-256 dedup → store file → Channel<Guid> enqueue
  → worker: PdfPig extract → chunk(500w/50 overlap) → nomic-embed-text(768d)
  → bulk-insert MaterialChunks
Student asks question → embed query → pgvector cosine top-5 → build prompt
  → llama3.2 stream → SSE to browser
```

---

## Tags Index

`#architecture` `#system` `#user-guide` `#development` `#skill` `#ai` `#rag` `#database` `#api` `#frontend` `#security` `#deployment`

---

## Change Log

See [[Changelog/CHANGELOG]] for all modifications to this knowledge base and the codebase.

---

*Source of truth: [[SRSmd/EduRAG_Technical_Specification]]*
