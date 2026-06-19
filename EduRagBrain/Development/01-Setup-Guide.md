---
tags: [development, setup, prerequisites, toolchain, mistralai, ai-provider]
created: 2026-06-18
updated: 2026-06-19
type: development
status: stable
aliases: [Setup Guide, Dev Environment, Prerequisites, AI Provider Switch]
---

# Setup Guide

> [[_HOME|← Home]] · [[00-Build-Order|← Build Order]]

## Local Dev Environment — What Runs Where

| Service | How | Port |
|---------|-----|------|
| PostgreSQL | **Local install** (not Docker) | **5433** |
| Ollama | Docker container | 11434 |
| API | `dotnet watch run` | 5000 |
| Frontend | `npm run dev` (Vite) | **5173** |

Local file paths: Ollama models → `E:\EduRagFiles\Ollama` · PDFs → `E:\EduRagFiles\EduRagPdfs`

---

## Required Tools

| Tool | Version | Install |
|------|---------|---------|
| .NET SDK | 8.x | https://dot.net |
| Node.js | 20.x LTS | https://nodejs.org |
| Docker Desktop | 4.x+ | https://docker.com |
| PostgreSQL | 16 with pgvector | Local install on port 5433 |
| Git | any | https://git-scm.com |
| VS Code or Rider | latest | IDE of choice |

---

## 1. Clone / Create Project

```bash
git clone <repo-url>
cd EduRag

# Or create fresh solution
mkdir src && cd src
dotnet new sln -n EduRAG
dotnet new classlib -n EduRAG.Domain         -o EduRAG.Domain
dotnet new classlib -n EduRAG.Application    -o EduRAG.Application
dotnet new classlib -n EduRAG.Infrastructure -o EduRAG.Infrastructure
dotnet new webapi   -n EduRAG.API            -o EduRAG.API

dotnet sln add EduRAG.Domain/EduRAG.Domain.csproj
dotnet sln add EduRAG.Application/EduRAG.Application.csproj
dotnet sln add EduRAG.Infrastructure/EduRAG.Infrastructure.csproj
dotnet sln add EduRAG.API/EduRAG.API.csproj

# Add project references
dotnet add EduRAG.Application/EduRAG.Application.csproj reference EduRAG.Domain/EduRAG.Domain.csproj
dotnet add EduRAG.Infrastructure/EduRAG.Infrastructure.csproj reference EduRAG.Domain/EduRAG.Domain.csproj
dotnet add EduRAG.Infrastructure/EduRAG.Infrastructure.csproj reference EduRAG.Application/EduRAG.Application.csproj
dotnet add EduRAG.API/EduRAG.API.csproj reference EduRAG.Domain/EduRAG.Domain.csproj
dotnet add EduRAG.API/EduRAG.API.csproj reference EduRAG.Application/EduRAG.Application.csproj
dotnet add EduRAG.API/EduRAG.API.csproj reference EduRAG.Infrastructure/EduRAG.Infrastructure.csproj
```

---

## 2. Install NuGet Packages

```bash
# Infrastructure
cd EduRAG.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Pgvector
dotnet add package Pgvector.EntityFrameworkCore
dotnet add package Dapper
dotnet add package Npgsql
dotnet add package BCrypt.Net-Next
dotnet add package PdfPig
cd ..

# API
cd EduRAG.API
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package FluentValidation.AspNetCore
dotnet add package Microsoft.EntityFrameworkCore.Design   # for EF migrations
cd ..
```

---

## 3. Start Ollama via Docker

PostgreSQL is a local install — do **not** start it via docker-compose.

```bash
# From project root — start Ollama only
docker-compose up -d ollama

# Confirm running
docker-compose ps
```

---

## 4. Pull Ollama Models

```bash
# One-time download — ~3 GB total
# Models persist in E:\EduRagFiles\Ollama between restarts
docker exec -it edurag-ollama-1 ollama pull nomic-embed-text
docker exec -it edurag-ollama-1 ollama pull llama3.2

# Verify both appear
docker exec -it edurag-ollama-1 ollama list
```

---

## 5. Run EF Core Migrations

```bash
cd src/EduRAG.API

# First-time tool install (once per machine)
dotnet tool install --global dotnet-ef

# Create initial migration
dotnet ef migrations add InitialCreate \
  --project ../EduRAG.Infrastructure \
  --startup-project .

# Apply
dotnet ef database update \
  --project ../EduRAG.Infrastructure \
  --startup-project .
```

---

## 6. Frontend Setup

```bash
cd frontend
npm install
# .env.local already exists with: VITE_API_BASE_URL=http://localhost:5000/api

# Install shadcn/ui
npx shadcn@latest init
# Select: TypeScript · Tailwind CSS · New York style · Violet · Yes to RSC · src/

# Install components
npx shadcn@latest add button card dialog select input textarea badge \
  progress tabs avatar scroll-area skeleton alert separator tooltip
```

---

## 7. Environment Config

`appsettings.json` already has the correct local values. `appsettings.Development.json` provides dev overrides (git-ignored):

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5433;Database=Edurag;Username=postgres;Password=Strong321#"
  },
  "Jwt": {
    "Secret": "dev-secret-key-minimum-32-chars-1234567890!!"
  },
  "Storage": {
    "BasePath": "E:\\EduRagFiles\\EduRagPdfs"
  }
}
```

---

## 8. Run Dev Servers

```powershell
# Terminal 1: API
cd src\EduRAG.API
dotnet watch run

# Terminal 2: Frontend
cd frontend
npm run dev
```

- API: http://localhost:5000/swagger
- Frontend: http://localhost:5173

---

## VS Code Extensions (Recommended)

```
C# Dev Kit
ESLint
Prettier
Tailwind CSS IntelliSense
REST Client (for .http files instead of Postman)
Docker
PostgreSQL (by Chris Kolkman)
```

---

---

## Switching AI Provider

The AI provider is controlled by `AI:Provider` in `appsettings.json` (or `appsettings.Development.json` for local dev). Two providers are supported: **Ollama** (default, local, free) and **MistralAI** (cloud, paid).

> Embedding dimensions differ: Ollama uses 768, MistralAI uses 1024. The database column must match the active provider. Switching requires a SQL migration script and re-vectorization of all PDFs.

### Switch from Ollama → MistralAI

**Step 1** — Run the SQL migration script (changes `vector(768)` → `vector(1024)`, clears all chunks, resets materials to Pending):

```powershell
psql -h localhost -p 5433 -U postgres -d Edurag -f scripts/migrate-to-mistralai.sql
```

**Step 2** — Update `appsettings.Development.json` (or `appsettings.json`):

```json
{
  "AI": {
    "Provider": "MistralAI",
    "EmbeddingDimensions": 1024,
    "MistralAI": {
      "ApiKey": "sk-your-mistral-api-key-here"
    }
  }
}
```

> Put the API key in `appsettings.Development.json` (git-ignored). Never commit it.

**Step 3** — Restart the API. `PendingMaterialsRequeueService` automatically re-queues all PDFs and `VectorizationWorker` re-vectorizes them using MistralAI.

**Step 4** — Monitor vectorization in the Admin portal or Swagger logs until all materials show **Completed**.

---

### Switch from MistralAI → Ollama

**Step 1** — Run the SQL migration script:

```powershell
psql -h localhost -p 5433 -U postgres -d Edurag -f scripts/migrate-to-ollama.sql
```

**Step 2** — Update config back to Ollama defaults:

```json
{
  "AI": {
    "Provider": "Ollama",
    "EmbeddingDimensions": 768
  }
}
```

**Step 3** — Ensure Ollama is running with `nomic-embed-text` and `llama3.2` models, then restart the API.

---

### Provider Feature Summary

| Feature | Ollama | MistralAI |
|---------|--------|-----------|
| Embed model | `nomic-embed-text` | `mistral-embed` |
| Chat model | `llama3.2` | `mistral-large-latest` |
| Embedding dims | 768 | 1024 |
| Internet required | No | Yes |
| API key required | No | Yes (`AI:MistralAI:ApiKey`) |
| Cost | Free | Per token |

---

## Related Docs

- [[00-Build-Order]] — phased build guide
- [[02-Testing]] — test scenarios
- [[../System/04-Configuration]] — all configuration values
- [[../System/05-Deployment]] — production deployment
- [[../Architecture/07-AI-Pipeline]] — RAG pipeline architecture and provider details
