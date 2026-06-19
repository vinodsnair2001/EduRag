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

## appsettings.json (template — no real secrets)

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5433;Database=Edurag;Username=postgres;Password=YOUR_DB_PASSWORD"
  },
  "AI": {
    "Provider": "Ollama",
    "EmbeddingDimensions": 768,
    "MistralAI": {
      "ApiKey": "YOUR_MISTRAL_API_KEY",
      "EmbedModel": "mistral-embed",
      "ChatModel": "mistral-large-latest",
      "BaseUrl": "https://api.mistral.ai"
    }
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

## appsettings.Development.json (overrides for dev — git-ignored)

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5433;Database=Edurag;Username=postgres;Password=<your-local-db-password>"
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

> **Never commit secrets.** `appsettings.Development.json` is git-ignored. Use it for local dev overrides including real DB passwords and the MistralAI API key.

---

## AI Provider Configuration

The active AI provider is controlled by `AI:Provider`. All other AI settings cascade from this choice.

### Using Ollama (default — local, free)

```json
"AI": {
  "Provider": "Ollama",
  "EmbeddingDimensions": 768
}
```

- Requires Ollama running locally with `nomic-embed-text` and `llama3.2` pulled
- Embedding column in database must be `vector(768)` (default after `InitialCreate` migration)

### Using MistralAI (cloud, requires API key)

```json
"AI": {
  "Provider": "MistralAI",
  "EmbeddingDimensions": 1024,
  "MistralAI": {
    "ApiKey": "YOUR_MISTRAL_API_KEY",
    "EmbedModel": "mistral-embed",
    "ChatModel": "mistral-large-latest",
    "BaseUrl": "https://api.mistral.ai"
  }
}
```

- `ApiKey` must be set (put it in `appsettings.Development.json`, never in source)
- Embedding column in database must be `vector(1024)` — run `scripts/migrate-to-mistralai.sql` first
- See [[../Development/01-Setup-Guide#switching-ai-provider|Switching AI Provider]] for full steps

### Switching Providers

Embedding dimensions differ between providers (768 vs 1024). The database column must match. See the SQL scripts in `scripts/`:

| Script | From → To | When to run |
|--------|-----------|-------------|
| `scripts/migrate-to-mistralai.sql` | Ollama → MistralAI | Before first run with MistralAI |
| `scripts/migrate-to-ollama.sql` | MistralAI → Ollama | When reverting to Ollama |

Both scripts delete all existing chunks and reset materials to Pending — the API re-vectorizes them automatically on next start.

---

## Environment Variables (Docker / Production)

Override any appsettings key using `__` as the section separator:

```bash
ConnectionStrings__Default="Host=postgres;Port=5432;Database=edurag;Username=postgres;Password=STRONG_PASS"
Jwt__Secret="REPLACE_WITH_MINIMUM_32_CHARACTER_RANDOM_KEY_HERE"
AllowedOrigins="https://edurag.yourdomain.com"

# Ollama
Ollama__BaseUrl="http://ollama:11434"

# OR MistralAI
AI__Provider="MistralAI"
AI__EmbeddingDimensions="1024"
AI__MistralAI__ApiKey="sk-..."
AI__MistralAI__BaseUrl="https://api.mistral.ai"
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
| `System.Net.Http.Json` | built-in (.NET 8) | JSON HTTP helpers (used by MistralAI + Ollama services) |

> No additional packages are required for MistralAI — it uses the same `HttpClient` + `System.Net.Http.Json` already present.

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
- [[../Architecture/07-AI-Pipeline]] — provider comparison and selection logic
