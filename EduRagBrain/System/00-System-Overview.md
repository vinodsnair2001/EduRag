---
tags: [system, overview, components, data-flow]
created: 2026-06-18
updated: 2026-06-18
type: system
status: stable
aliases: [System Overview]
---

# System Overview

> [[_HOME|← Home]]

## What EduRAG Does

EduRAG lets school administrators upload PDF study materials and lets students chat with those materials using AI. All AI runs locally — no paid cloud API calls.

**Two user types:**

| Role | Can Do |
|------|--------|
| Admin | Manage classes/subjects/chapters, upload PDFs, manage user accounts, view vectorization status |
| Student | Select class+subject, open AI chat session, ask questions, request practice questions, submit answers for grading |

---

## Runtime Component Map

```
┌─────────────────────────────────────────────────────────────────────┐
│  Docker host (single machine)                                        │
│                                                                      │
│  ┌────────────┐   HTTP:3000   ┌──────────────────────────────────┐  │
│  │  Browser   │──────────────►│  React 18 SPA  (Nginx :80)        │  │
│  └────────────┘               └──────────────┬───────────────────┘  │
│                                              │ /api/* proxy          │
│                               ┌──────────────▼───────────────────┐  │
│                               │  ASP.NET Core 8 API  (:8080)      │  │
│                               │  ┌──────────────────────────┐    │  │
│                               │  │ AuthController            │    │  │
│                               │  │ AdminController           │    │  │
│                               │  │ StudentController         │    │  │
│                               │  │ ChatController (SSE)      │    │  │
│                               │  └──────────────────────────┘    │  │
│                               │  ┌──────────────────────────┐    │  │
│                               │  │ VectorizationWorker       │    │  │
│                               │  │ Channel<Guid> queue       │    │  │
│                               │  └──────────────────────────┘    │  │
│                               └──────┬───────────────┬───────────┘  │
│                                      │               │               │
│              ┌───────────────────────┘               │               │
│              ▼                                        ▼               │
│  ┌───────────────────────┐           ┌───────────────────────────┐  │
│  │  PostgreSQL 16         │           │  Ollama  (:11434)          │  │
│  │  + pgvector extension  │           │  nomic-embed-text          │  │
│  │  Tables:               │           │  llama3.2                  │  │
│  │   Classes/Subjects/    │           └───────────────────────────┘  │
│  │   Chapters/Materials/  │                                           │
│  │   MaterialChunks       │                                           │
│  │   (vector(768))        │                                           │
│  │   Users/Sessions/Msgs  │                                           │
│  └───────────────────────┘                                           │
│                                                                      │
│  /var/edurag/materials/   (PDF file storage volume)                  │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Core Data Flows

### 1. Admin Login

```
POST /api/auth/login {email, password}
→ BCrypt.Verify(password, hash) 
→ Generate JWT {userId, role=Admin, exp=8h}
→ Return {token, role, fullName}
```

### 2. PDF Upload + Vectorization

```
POST /api/admin/upload  (multipart PDF)
→ SHA-256 hash → dedup check
→ Save to /storage/materials/
→ INSERT StudyMaterial (status=Pending)
→ Channel.Writer.WriteAsync(materialId)
                              ↓  (async, background)
→ PdfPig extract → 500-word chunks
→ nomic-embed-text → float[768] per chunk
→ INSERT MaterialChunks with Embedding
→ UPDATE StudyMaterial status=Completed
```

### 3. Student Chat (RAG)

```
POST /api/chat/sessions/{id}/messages {content}
→ Verify session ownership
→ nomic-embed-text(question) → float[768]
→ pgvector cosine search: top-5 chunks (filtered by classId+subjectId)
→ Build system prompt with chunks as CONTEXT
→ llama3.2 streaming chat → SSE tokens to browser
→ Save assistant message + source chunk IDs
```

---

## Port Map

| Port | Service |
|------|---------|
| 3000 | React frontend (nginx) |
| 5000 | API proxy from host (mapped to :8080 inside container) |
| 5432 | PostgreSQL |
| 11434 | Ollama |

---

## Storage Volumes

| Volume | Content |
|--------|---------|
| `pgdata` | PostgreSQL data directory |
| `ollama_models` | Downloaded Ollama models (~4-8 GB) |
| `materials` | Uploaded PDF files |

---

## Related Docs

- [[../Architecture/00-Overview]] — architecture layer diagram
- [[01-Database-Schema]] — full DDL
- [[02-API-Reference]] — all endpoints
- [[05-Deployment]] — docker-compose startup
