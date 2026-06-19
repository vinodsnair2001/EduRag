---
tags: [system, configuration, appsettings, environment, docker]
created: 2026-06-18
updated: 2026-06-19
type: system
status: stable
aliases: [Configuration, appsettings, Environment Variables]
---

# Configuration

> [[_HOME|← Home]] · [[00-System-Overview|← System Overview]]

## Local Development Environment

| Service | How it runs | Host | Port |
|---------|-------------|------|------|
| PostgreSQL | **Local Windows install** | localhost | **5433** |
| Ollama | **Local Windows app** | localhost | 11434 |
| API | `dotnet watch run` | localhost | 5000 |
| Frontend | `npm run dev` (Vite) | localhost | **5173** |

Docker is **not used** for local development. Both PostgreSQL and Ollama run as local Windows services.

**Local file paths:**
- Uploaded PDFs: `E:\EduRagFiles\EduRagPdfs`

---

## appsettings.json (actual values)

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5433;Database=Edurag;Username=postgres;Password=Strong321#"
  },
  "Jwt": {
    "Secret":         "REPLACE_WITH_MINIMUM_32_CHARACTER_RANDOM_KEY_HERE_!!",
    "Issuer":         "EduRAG",
    "Audience":       "EduRAG",
    "ExpiryMinutes":  480
  },
  "Ollama": {
    "BaseUrl":    "http://localhost:11434",
    "EmbedModel": "nomic-embed-text",
    "ChatModel":  "llama3.2",
    "ModelsPath": "E:\\EduRagFiles\\Ollama"
  },
  "Storage": {
    "BasePath": "E:\\EduRagFiles\\EduRagPdfs"
  },
  "Chunking": {
    "ChunkSize":    500,
    "ChunkOverlap": 50
  },
  "RAG": {
    "TopKChunks": 5
  },
  "AllowedOrigins": "http://localhost:5173"
}
```

## appsettings.Development.json (overrides for dev)

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
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

> **Never commit secrets.** `appsettings.Development.json` is git-ignored. Use it for local dev overrides.

---

## Environment Variables (Docker / Production)

Override any appsettings key using `__` as the section separator:

```bash
ConnectionStrings__Default="Host=postgres;Port=5432;Database=edurag;Username=postgres;Password=STRONG_PASS"
Jwt__Secret="REPLACE_WITH_MINIMUM_32_CHARACTER_RANDOM_KEY_HERE"
Ollama__BaseUrl="http://ollama:11434"
Storage__BasePath="/var/edurag/materials"
AllowedOrigins="https://edurag.yourdomain.com"
```

---

## docker-compose.yml (actual — Ollama only in local dev)

PostgreSQL is a local install; only Ollama runs in Docker for local development.

```yaml
services:

  ollama:
    image: ollama/ollama:latest
    ports:
      - '11434:11434'
    volumes:
      - E:/EduRagFiles/Ollama:/root/.ollama

  api:
    build:
      context: ./src
      dockerfile: EduRAG.API/Dockerfile
    ports:
      - '5000:8080'
    depends_on:
      - ollama
    environment:
      ConnectionStrings__Default: "Host=host.docker.internal;Port=5433;Database=Edurag;Username=postgres;Password=Strong321#"
      Ollama__BaseUrl: "http://ollama:11434"
      Jwt__Secret:     "REPLACE_WITH_ACTUAL_32_CHAR_PRODUCTION_SECRET!!"
      Storage__BasePath: "/var/edurag/materials"
      AllowedOrigins: "http://localhost:3000"
    volumes:
      - E:/EduRagFiles/EduRagPdfs:/var/edurag/materials
    extra_hosts:
      - "host.docker.internal:host-gateway"

  frontend:
    build: ./frontend
    ports:
      - '3000:80'
    depends_on:
      - api
```

---

## NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.EntityFrameworkCore` | 8.x | ORM write ops |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 8.x | PostgreSQL EF Core provider |
| `Pgvector` | 0.2+ | pgvector .NET support |
| `Dapper` | 2.1 | Micro-ORM read ops |
| `Npgsql` | 8.x | ADO.NET PostgreSQL driver |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.x | JWT middleware |
| `BCrypt.Net-Next` | 4.x | Password hashing |
| `PdfPig` | 0.1.9 | PDF text extraction |
| `System.Threading.Channels` | built-in | In-memory job queue |
| `FluentValidation.AspNetCore` | 11.x | Request validation |

---

## Frontend .env

```bash
# frontend/.env.local
VITE_API_BASE_URL=http://localhost:5000/api
```

Vite exposes via `import.meta.env.VITE_API_BASE_URL`.

---

## Logging Configuration

```json
"Logging": {
  "LogLevel": {
    "Default":                      "Information",
    "Microsoft.AspNetCore":         "Warning",
    "Microsoft.EntityFrameworkCore": "Warning"
  }
}
```

In production, redirect logs to a file or structured logging sink (Serilog recommended).

---

## Related Docs

- [[05-Deployment]] — how to apply these configs during deployment
- [[03-Security]] — JWT secret requirements
- [[../Architecture/05-API-Layer]] — DI registration that consumes these values
