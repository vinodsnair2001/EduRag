---
tags: [architecture, overview, edurag]
created: 2026-06-18
updated: 2026-06-18
type: architecture
status: stable
aliases: [Architecture Overview]
---

# Architecture Overview

> [[_HOME|← Home]]

## System at a Glance

EduRAG is a four-layer Clean Architecture application. All layers communicate inward; no inner layer knows about outer layers.

```
┌─────────────────────────────────────────────────────┐
│  EduRAG.API  (Controllers · Middleware · DI)         │  ← HTTP boundary
├─────────────────────────────────────────────────────┤
│  EduRAG.Infrastructure  (EF Core · Dapper · Ollama) │  ← external adapters
├─────────────────────────────────────────────────────┤
│  EduRAG.Application  (Use-Cases · Interfaces · DTOs) │  ← business logic
├─────────────────────────────────────────────────────┤
│  EduRAG.Domain  (Entities · Enums · Events)          │  ← pure domain
└─────────────────────────────────────────────────────┘
```

**Dependency rule:** arrows point inward only. Infrastructure implements Application interfaces; Application never imports Infrastructure.

---

## Component Map

```
                  ┌──────────┐
     Browser ────►│ React 18 │
                  └────┬─────┘
                       │ HTTP / SSE
                  ┌────▼────────────────────────────────────────┐
                  │           ASP.NET Core 8 API                │
                  │  AuthController  AdminController  ChatCtrl   │
                  └────┬───────────────────┬────────────────────┘
              EF Core  │             Dapper│
           (Write ops) │          (Read ops│
              ┌────────▼───────┐  ┌────────▼────────────────┐
              │  PostgreSQL 16  │  │  pgvector HNSW index     │
              │  + pgvector     │  │  (cosine similarity)     │
              └────────────────┘  └─────────────────────────┘
                                          ▲
                       Embed / Chat       │
              ┌────────────────────┐      │
              │   Ollama Server     │──────┘
              │  nomic-embed-text   │  768-dim embeddings
              │  llama3.2           │  streaming chat tokens
              └────────────────────┘
                       ▲
              ┌────────┴────────────────────────────────────┐
              │  VectorizationWorker (BackgroundService)     │
              │  Channel<Guid> queue · PdfPig extraction     │
              └─────────────────────────────────────────────┘
```

---

## Patterns Used

| Pattern | Where | Why |
|---------|-------|-----|
| Clean Architecture | All layers | Testability, dependency inversion |
| CQRS (lite) | Infrastructure | EF Core writes, Dapper reads — no change-tracking overhead on reads |
| Repository pattern | Infrastructure | Abstracts persistence from use-cases |
| Background job via Channel<T> | VectorizationWorker | No broker needed; in-process queue is sufficient |
| SSE streaming | ChatController | Low-latency token delivery to browser without WebSocket complexity |
| RAG (Retrieval-Augmented Generation) | ChatUseCase | Grounds LLM answers in uploaded PDF content |

---

## Cross-Cutting Concerns

- **Auth:** JWT Bearer tokens, role-based (`Admin` / `Student`), validated on every request
- **Logging:** ASP.NET Core built-in `ILogger<T>` throughout
- **Error handling:** Global exception-handler middleware → structured JSON error responses
- **Validation:** DataAnnotations / FluentValidation at API boundary
- **Rate limiting:** Applied on `/api/chat` endpoints

---

## Related Docs

- [[01-Clean-Architecture]] — layer rules and solution layout
- [[02-Domain-Layer]] — entities and enums
- [[07-AI-Pipeline]] — RAG and Ollama details
- [[../System/00-System-Overview]] — runtime component map
