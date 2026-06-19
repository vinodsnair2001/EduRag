---
tags: [development, build-order, phases, checklist]
created: 2026-06-18
updated: 2026-06-18
type: development
status: stable
aliases: [Build Order, Build Phases, Implementation Plan]
---

# Build Order

> [[_HOME|← Home]]

Follow this order strictly. Each phase depends on the previous being complete.

---

## Phase 1 — Infrastructure Foundation

- [ ] 1. Run `docker-compose up -d postgres ollama`
- [ ] 2. Pull Ollama models:
  ```bash
  ollama pull nomic-embed-text
  ollama pull llama3.2
  ```
- [ ] 3. Create `EduRAG.sln` and four C# projects:
  - `EduRAG.Domain`
  - `EduRAG.Application`
  - `EduRAG.Infrastructure`
  - `EduRAG.API`
- [ ] 4. Write all [[../Architecture/02-Domain-Layer|Domain entities]] and enums
- [ ] 5. Write all [[../Architecture/03-Application-Layer|Application interfaces]]
- [ ] 6. Write `AppDbContext` and all `IEntityTypeConfiguration<T>` classes
- [ ] 7. Create EF Core initial migration and run `database update`
- [ ] 8. Seed admin user (BCrypt hash of `Admin@123`, work factor 11)

**Gate:** `SELECT COUNT(*) FROM "AppUsers"` returns 1.

---

## Phase 2 — Write Operations + Auth

- [ ] 9. Implement EF Core repositories:
  `ClassRepository`, `SubjectRepository`, `ChapterRepository`,
  `StudyMaterialRepository`, `MaterialChunkRepository`, `ChatRepository`
- [ ] 10. Implement `JwtService` (generate + validate tokens)
- [ ] 11. Implement `AuthController` (login, register)
- [ ] 12. Implement `AdminController` CRUD for Classes, Subjects, Chapters
- [ ] 13. **Test:** Postman — login, create class, create subject, create chapter

**Gate:** JWT returned from `/auth/login`, CRUD operations succeed.

---

## Phase 3 — Read Operations

- [ ] 14. Implement Dapper query classes:
  `ClassQueries`, `SubjectQueries`, `ChapterQueries`, `MaterialQueries`, `ChatQueries`
- [ ] 15. Wire Dapper reads into `AdminController` GET endpoints
- [ ] 16. Implement `StudentController` (class/subject selection)
- [ ] **Test:** GET `/admin/classes` returns data from Dapper

**Gate:** Admin list endpoints work; student selection endpoints work.

---

## Phase 4 — File Upload + AI Pipeline

- [ ] 17. Implement `LocalFileStorageService` (folder creation + save)
- [ ] 18. Implement Upload endpoint in `AdminController`
- [ ] 19. Implement `PdfProcessingService` (PdfPig extract + chunking)
- [ ] 20. Implement `OllamaAIService` (embed + streaming chat)
- [ ] 21. Implement `VectorizationProcessor`
- [ ] 22. Implement `VectorizationWorker` (BackgroundService + Channel)
- [ ] 23. **Test:** Upload a PDF → check `MaterialChunks` table is populated

**Gate:** `SELECT COUNT(*) FROM "MaterialChunks"` returns rows after upload; status = Completed.

---

## Phase 5 — Chat + RAG

- [ ] 24. Implement `VectorSearchService` (pgvector cosine query)
- [ ] 25. Implement `ChatUseCase` (full RAG pipeline: embed → search → prompt → stream)
- [ ] 26. Implement `ChatController` (create session, streaming message endpoint)
- [ ] 27. Implement `ChatQueries` (Dapper, load history)
- [ ] 28. **Test:** Start chat session, ask question, verify answer is from PDF context

**Gate:** SSE stream returns tokens; answer references uploaded material; `SourceChunkIds` saved.

---

## Phase 6 — Frontend

- [ ] 29. Set up Vite + React + TypeScript + Tailwind + shadcn/ui
  - Run `npx shadcn@latest init` with New York style, Violet base colour
  - Install components: button, card, dialog, select, input, textarea, badge,
    progress, tabs, avatar, scroll-area, skeleton, alert, separator, tooltip
- [ ] 30. Implement `AuthContext`, `useAuth` hook, `ProtectedRoute`
- [ ] 31. Build `LoginPage` (tab: Admin vs Student)
- [ ] 32. Build Admin portal:
  - `AdminDashboard`, `ClassListPage`, `ClassDetailPage`
  - `UploadMaterialPage`, `MaterialListPage`, `UserManagementPage`
- [ ] 33. Build Student portal:
  - `ClassSubjectSelectPage` (colourful card grid, age-appropriate design)
- [ ] 34. Build `ChatPage` with:
  - SSE streaming, real-time token append
  - `PracticeCard` component with answer submission + verdict
  - Chat history on load
  - "Quiz me" shortcut button in `ChatInput`

**Gate:** Full end-to-end: login → select class → chat → receive streamed answer.

---

## Phase 7 — Polish + Testing

- [ ] 35. Add global exception handler middleware
- [ ] 36. Add FluentValidation for all request DTOs
- [ ] 37. Add rate limiting on `/api/chat` endpoints
- [ ] 38. Write integration tests:
  - Upload → vectorize → chat pipeline
  - Auth token validation
  - Chat session ownership enforcement
- [ ] 39. Frontend:
  - Loading states (Skeleton, Spinner)
  - Error handling (Toast, Alert)
  - Empty states (friendly illustrations)
  - Mobile responsiveness
- [ ] 40. Dockerize API and frontend; full `docker-compose up` smoke test

**Gate:** All tests pass; docker-compose up brings full working system.

---

## Related Docs

- [[01-Setup-Guide]] — environment prerequisites
- [[02-Testing]] — test scenarios per phase
- [[../Architecture/00-Overview]] — architecture reference while building
