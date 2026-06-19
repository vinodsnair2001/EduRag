---
tags: [architecture, clean-architecture, layers, ddd]
created: 2026-06-18
updated: 2026-06-18
type: architecture
status: stable
aliases: [Clean Architecture, Layer Rules]
---

# Clean Architecture

> [[_HOME|← Home]] · [[00-Overview|← Overview]]

## Dependency Rule

```
EduRAG.API
    └── EduRAG.Infrastructure
            └── EduRAG.Application
                    └── EduRAG.Domain   ← zero dependencies
```

**No layer may reference a layer outside its boundary.** Infrastructure may never be imported by Application. Application may never be imported by Domain.

---

## Solution Layout

```
EduRAG.sln
├── src/
│   ├── EduRAG.Domain/               # Layer 1 — innermost, zero NuGet deps
│   │   ├── Entities/                # Class, Subject, Chapter, StudyMaterial,
│   │   │                            # MaterialChunk, AppUser, ChatSession, ChatMessage
│   │   ├── Enums/                   # UserRole, MessageRole, VectorizationStatus
│   │   └── Events/                  # Domain events (records) — future use
│   │
│   ├── EduRAG.Application/          # Layer 2
│   │   ├── Interfaces/              # IAIService, IVectorSearchService,
│   │   │                            # IFileStorageService, all IXxxRepository
│   │   ├── UseCases/
│   │   │   ├── Admin/               # ManageClassUseCase, ManageSubjectUseCase,
│   │   │   │                        # ManageChapterUseCase, UploadMaterialUseCase
│   │   │   └── Student/             # ChatUseCase
│   │   ├── DTOs/                    # Request / Response objects
│   │   └── Common/                  # Result<T>, PaginatedList<T>
│   │
│   ├── EduRAG.Infrastructure/       # Layer 3
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs      # EF Core DbContext
│   │   │   ├── Configurations/      # IEntityTypeConfiguration<T> per entity
│   │   │   ├── Repositories/        # EF Core — Insert/Update/Delete only
│   │   │   └── Queries/             # Dapper — SELECT only
│   │   ├── Services/
│   │   │   ├── AI/                  # OllamaAIService, PdfProcessingService,
│   │   │   │                        # VectorizationProcessor
│   │   │   └── File/                # LocalFileStorageService
│   │   └── BackgroundJobs/          # VectorizationWorker
│   │
│   └── EduRAG.API/                  # Layer 4 — outermost
│       ├── Controllers/             # AuthController, AdminController,
│       │                            # StudentController, ChatController
│       ├── Middleware/              # JWT auth, global exception handler
│       └── Extensions/             # ServiceRegistration (DI wiring)
│
└── frontend/                        # React 18 + TypeScript (Vite)
    └── src/
        ├── admin/                   # Admin portal pages + components
        ├── student/                 # Student portal pages + components
        ├── auth/                    # LoginPage, AuthContext
        └── shared/                  # ProtectedRoute, StatusBadge, API client
```

---

## Project Reference Rules

| Project | Allowed References | Forbidden |
|---------|--------------------|-----------|
| `EduRAG.Domain` | None | Everything |
| `EduRAG.Application` | Domain | Infrastructure, API |
| `EduRAG.Infrastructure` | Domain, Application | API |
| `EduRAG.API` | All three above | None |

---

## NuGet Package Ownership

| Package | Project |
|---------|---------|
| `Microsoft.EntityFrameworkCore` | Infrastructure |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | Infrastructure |
| `Pgvector` | Infrastructure |
| `Dapper` | Infrastructure |
| `Npgsql` | Infrastructure |
| `BCrypt.Net-Next` | Infrastructure |
| `PdfPig` | Infrastructure |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | API |
| `FluentValidation.AspNetCore` | API |

---

## Key Design Decisions

1. **CQRS-lite** — EF Core for writes (change tracking useful), Dapper for reads (faster, no overhead). No MediatR or command bus — use-cases called directly from controllers.
2. **No AutoMapper** — manual mapping in use-cases; explicit is safer at this scale.
3. **No separate Read Models** — Dapper DTOs are the read model; no separate projection layer needed.
4. **In-process queue** — `Channel<Guid>` rather than RabbitMQ/Azure Service Bus. Sufficient for single-node deployment; easy to swap later.

---

## Related Docs

- [[02-Domain-Layer]] — entity details
- [[03-Application-Layer]] — use-case and interface details
- [[04-Infrastructure-Layer]] — EF Core and Dapper implementation
- [[05-API-Layer]] — controller and middleware details
