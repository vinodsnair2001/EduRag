---
tags: [system, deployment, docker, migrations, setup]
created: 2026-06-18
updated: 2026-06-18
type: system
status: stable
aliases: [Deployment, Docker, Setup]
---

# Deployment

> [[_HOME|← Home]] · [[00-System-Overview|← System Overview]]

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| Docker Desktop | 4.x+ | Containers |
| .NET SDK | 8.x | Backend build |
| Node.js | 20.x+ | Frontend build |
| Git | any | Source control |

---

## Step-by-Step: First Deploy

### 1. Start Infrastructure

```bash
# From project root
docker-compose up -d postgres ollama
```

Wait ~30 seconds for PostgreSQL to be ready.

### 2. Pull Ollama Models

This only needs to happen once. Models persist in the `ollama_models` volume.

```bash
# Pull embedding model (~270 MB)
docker exec -it edurag_ollama_1 ollama pull nomic-embed-text

# Pull chat model (~2 GB for llama3.2:3b or ~4.7 GB for llama3.2:8b)
docker exec -it edurag_ollama_1 ollama pull llama3.2

# Verify
docker exec -it edurag_ollama_1 ollama list
```

### 3. Run EF Core Migrations

```bash
cd src/EduRAG.API

# Apply migrations to database
dotnet ef database update \
  --connection "Host=localhost;Port=5432;Database=edurag;Username=postgres;Password=your_password"
```

Or from the Infrastructure project:

```bash
dotnet ef database update \
  --project ../EduRAG.Infrastructure \
  --startup-project .
```

### 4. Seed Admin User

After migrations, run the seed SQL or use the `DbInitializer` class:

```bash
# Generate BCrypt hash for Admin@123
dotnet script tools/hash_password.csx "Admin@123"

# Insert seed user (replace hash)
psql -h localhost -U postgres -d edurag -c "
INSERT INTO \"AppUsers\" (\"Email\",\"FullName\",\"PasswordHash\",\"Role\")
VALUES ('admin@edurag.com','System Admin','\$2a\$11\$...hash...',0)
ON CONFLICT DO NOTHING;"
```

### 5. Build and Start API

```bash
cd src/EduRAG.API
dotnet run --environment Development
# API listens on http://localhost:5000
```

### 6. Build and Start Frontend

```bash
cd frontend
npm install
npm run dev
# Frontend on http://localhost:3000
```

---

## Full Docker Deployment

```bash
# Build all services
docker-compose build

# Start everything
docker-compose up -d

# Check all are running
docker-compose ps
```

After `up`, the services are:
- Frontend: http://localhost:3000
- API: http://localhost:5000
- Postgres: localhost:5432
- Ollama: http://localhost:11434

---

## EF Core Migration Commands

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> \
  --project src/EduRAG.Infrastructure \
  --startup-project src/EduRAG.API

# Apply latest migration
dotnet ef database update \
  --project src/EduRAG.Infrastructure \
  --startup-project src/EduRAG.API

# Revert last migration
dotnet ef database update <PreviousMigrationName> \
  --project src/EduRAG.Infrastructure \
  --startup-project src/EduRAG.API

# Generate SQL script (for production review)
dotnet ef script \
  --project src/EduRAG.Infrastructure \
  --startup-project src/EduRAG.API \
  --output migration.sql
```

---

## Health Checks

Add to API for production monitoring:

```
GET /health        → overall health
GET /health/ready  → DB + Ollama connectivity
GET /health/live   → always 200 if process is alive
```

---

## Updating Ollama Models

```bash
# Pull newer version of a model
docker exec -it edurag_ollama_1 ollama pull llama3.2

# List installed models and sizes
docker exec -it edurag_ollama_1 ollama list
```

---

## Backing Up Data

```bash
# PostgreSQL dump
docker exec -it edurag_postgres_1 \
  pg_dump -U postgres edurag > backup_$(date +%Y%m%d).sql

# Restore
docker exec -i edurag_postgres_1 \
  psql -U postgres edurag < backup_20260618.sql
```

---

## Related Docs

- [[04-Configuration]] — all config values needed for deployment
- [[06-Troubleshooting]] — deployment errors and fixes
- [[../Development/00-Build-Order]] — phased build checklist
