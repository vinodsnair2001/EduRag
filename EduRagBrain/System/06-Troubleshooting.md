---
tags: [system, troubleshooting, errors, debug]
created: 2026-06-18
updated: 2026-06-19
type: system
status: stable
aliases: [Troubleshooting, Errors, FAQ Technical]
---

# Troubleshooting

> [[_HOME|← Home]] · [[00-System-Overview|← System Overview]]

## Ollama Issues

### Error: `404 Not Found` on `/api/embeddings`

**Cause:** Model not pulled yet.

```bash
# Fix
docker exec -it edurag_ollama_1 ollama list
docker exec -it edurag_ollama_1 ollama pull nomic-embed-text
docker exec -it edurag_ollama_1 ollama pull llama3.2
```

### Error: `connection refused` to `http://localhost:11434`

**Cause:** Ollama container not running.

```bash
docker-compose up -d ollama
docker-compose logs ollama
```

### Chat response is very slow

**Cause:** llama3.2 running on CPU only. GPU not available or not passed through.

```yaml
# docker-compose.yml — uncomment GPU passthrough
deploy:
  resources:
    reservations:
      devices:
        - driver: nvidia
          count: 1
          capabilities: [gpu]
```

---

## Database / pgvector Issues

### Error: `column "Embedding" is of type vector but expression is of type text`

**Cause:** pgvector extension not installed, or `UseVector()` missing in DI.

```bash
# Fix 1: ensure extension installed
psql -U postgres -d edurag -c "CREATE EXTENSION IF NOT EXISTS vector;"

# Fix 2: ensure DI registration has UseVector()
```

```csharp
o.UseNpgsql(cs, npg => npg.UseVector())   // ← required
```

### Error: `type "vector" does not exist` in EF migration

**Cause:** Pgvector NuGet package not installed, or `mb.HasPostgresExtension("vector")` missing.

```csharp
// AppDbContext.OnModelCreating
mb.HasPostgresExtension("vector");   // ← required
```

### Error: `PostgresException: 22000: expected 1024 dimensions, not 768` (EF Core column type mismatch)

This error fires in `VectorizationWorker` (EF Core INSERT path) when the `AI:Provider` is `MistralAI` (1024-dim embeddings) but the EF Core entity configuration still declares `HasColumnType("vector(768)")`.

**Root cause:** `MaterialChunkConfiguration.HasColumnType` was hardcoded to `vector(768)`. Npgsql uses this annotation when generating INSERT parameters, causing dimension mismatches when the actual DB column is `vector(1024)`.

**Fix (applied in branch `feature-Vectorisation-based-on-class-subject-chapter`):**

`AppDbContext` now injects `IConfiguration` and overrides the column type in `OnModelCreating` after `ApplyConfigurationsFromAssembly`:

```csharp
mb.Entity<MaterialChunk>()
  .Property(x => x.Embedding)
  .HasColumnType($"vector({_embeddingDimensions})");  // _embeddingDimensions from AI:EmbeddingDimensions
```

This ensures the EF model always matches the configured provider dimension without any code change when switching between Ollama (768) and MistralAI (1024).

**If the error persists after the fix:** verify the DB column type matches `AI:EmbeddingDimensions`:

```sql
SELECT udt_name FROM information_schema.columns
WHERE table_name = 'MaterialChunks' AND column_name = 'Embedding';
-- should show: vector
-- check dimension:
SELECT typmod FROM pg_attribute a
JOIN pg_class c ON a.attrelid = c.oid
WHERE c.relname = 'MaterialChunks' AND a.attname = 'Embedding';
-- typmod - 4 = dimension (e.g. 1028 - 4 = 1024)
```

If the column dimension does not match the config, run the appropriate SQL migration script in `scripts/`.

### Error: `PostgresException: 22000: expected 768 dimensions, not 1536` (locale bug — most common)

If the diag endpoint shows `embedding: ok (768 dims)` but the vector search still fails with `expected 768 dimensions, not 1536`, this is a **culture/locale bug**, not a real dimension mismatch. `768 × 2 = 1536` is the giveaway.

`VectorSearchService` builds the query vector as a comma-delimited string literal. On a machine whose current culture uses a **comma decimal separator** (German, French, and many European/Indian locales), `float.ToString()` renders `0.5` as `0,5`. Postgres then reads each decimal-comma as a separator, parsing 1536 tokens from a 768-element array.

```csharp
// Fix — always format with InvariantCulture
var vectorLiteral = "[" +
    string.Join(",", queryEmbedding.Select(f => f.ToString("R", CultureInfo.InvariantCulture))) +
    "]";
```

Storage is unaffected because EF Core's `Pgvector.Vector` converter serializes with invariant culture — only the manual query literal in `VectorSearchService` had the bug.

### Error: `PostgresException: 22000: different vector dimensions 768 and 1536`

This error fires in `VectorSearchService.SearchAsync` when the query vector and stored vectors genuinely have different dimensions. Distinct root causes:

**Cause 1: DB column created as `vector(1536)` (most common)**

The `MaterialChunks."Embedding"` column was created with the wrong dimension before the EF Core migration ran. EF Core skips `CREATE TABLE` if the table already exists, leaving the wrong type in place.

```sql
-- Diagnose
SELECT udt_name FROM information_schema.columns
WHERE table_name = 'MaterialChunks' AND column_name = 'Embedding';

SELECT vector_dims("Embedding"), COUNT(*)
FROM "MaterialChunks" GROUP BY vector_dims("Embedding");

-- Fix (run on database Edurag, port 5433)
TRUNCATE TABLE "MaterialChunks";
ALTER TABLE "MaterialChunks" ALTER COLUMN "Embedding" TYPE vector(768);
DELETE FROM "StudyMaterials";  -- removes hash lock so PDFs can be re-uploaded
```

Restart the API after running, then re-upload PDFs.

**Cause 2: Dapper connection missing pgvector type mapping**

A raw `new NpgsqlConnection(cs)` does not share the `NpgsqlDataSource` that EF Core configured with `UseVector()`. Npgsql resolves the `vector` type OID from the PostgreSQL catalog and may use stale/wrong dimension metadata.

Fix: use a shared `NpgsqlDataSource` for both EF Core and Dapper (see [[../Architecture/04-Infrastructure-Layer]] → NpgsqlDataSource section), and pass a native `Vector` object — not a string with `::vector` cast — to Dapper.

**Cause 3: Wrong Ollama model**

If `Ollama:EmbedModel` in `appsettings.json` is changed to a model with different output dimensions (e.g. `mxbai-embed-large` = 1024 dims), stored and query embeddings will mismatch.

- `nomic-embed-text` → always **768 dims** — the only supported model
- Do not change without also `ALTER COLUMN "Embedding" TYPE vector(NEW_DIM)` and re-vectorizing all materials

**Diagnostic endpoint** (anonymous, use during debugging):
```
GET http://localhost:{port}/api/chat/diag
```
Returns embedding dimension from Ollama and whether a vector search succeeds for classId=1, subjectId=1.

### Migration fails: `relation "Classes" already exists`

**Cause:** Schema was created manually but migrations haven't been applied.

```bash
# Option 1: drop and recreate (dev only)
psql -U postgres -d edurag -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"
dotnet ef database update

# Option 2: mark baseline migration as applied
dotnet ef database update InitialMigration --no-build
```

---

## Vectorization Issues

### Status stays `Pending` — never processes

**Cause 1:** `Channel<Guid>` registered as `Scoped` instead of `Singleton`.

```csharp
// Fix — must be Singleton
services.AddSingleton(Channel.CreateUnbounded<Guid>());
services.AddHostedService<VectorizationWorker>();
```

**Cause 2:** `VectorizationWorker` not registered as `HostedService`.

**Cause 3:** Exception in `VectorizationProcessor` — check logs.

**Cause 4:** API restarted with materials already in `Pending`/`Failed` state.

`Channel<Guid>` is in-memory. On restart the channel is empty, so no pending materials are ever processed. `PendingMaterialsRequeueService` fixes this by re-queuing at startup. Ensure it is registered in `ServiceRegistration.cs`:

```csharp
services.AddHostedService<VectorizationWorker>();
services.AddHostedService<PendingMaterialsRequeueService>();
```

If the service is missing or `StudyMaterials` were deleted, re-upload the PDF from the Admin panel.

### Status shows `Failed`

Check `StudyMaterial.VectorizationError` column for the exception message.

```sql
SELECT "Id", "OriginalFileName", "VectorizationError"
FROM "StudyMaterials"
WHERE "VectorizationStatus" = 3;  -- 3 = Failed
```

### PDF text extraction returns empty / very little text

**Cause:** PDF is a scanned image, not text-based.

**Fix (future):** Add OCR via Tesseract.Net:

```csharp
// Add as fallback in PdfProcessingService when page text is empty
using var engine = new TesseractEngine("./tessdata", "eng");
// ... rasterize page → run OCR
```

---

## Chat Issues

### Chat returns "I couldn't find this in the uploaded study material"

This is the expected response when no relevant chunks are found. Diagnose:

```sql
-- Check chunks exist for the class/subject
SELECT COUNT(*), MIN("Score") 
FROM "MaterialChunks"
WHERE "ClassId" = 1 AND "SubjectId" = 10;

-- If 0 rows: vectorization didn't complete
SELECT "VectorizationStatus", "VectorizationError"
FROM "StudyMaterials"
WHERE "ClassId" = 1 AND "SubjectId" = 10;
```

### SSE stream not received in browser / blank response

**Cause 1:** `Content-Type: text/event-stream` not set.

```csharp
Response.Headers.Append("Content-Type", "text/event-stream");
Response.Headers.Append("Cache-Control", "no-cache");
```

**Cause 2:** Response buffering enabled in IIS or nginx.

```nginx
# nginx fix
location /api/chat/ {
    proxy_pass http://api:8080;
    proxy_buffering off;
    proxy_cache off;
    proxy_read_timeout 300s;
}
```

### 403 on chat message

**Cause:** Student is trying to send message to a session they don't own.

Check: `ChatSession.UserId` must equal the `sub` claim in the JWT.

---

## Frontend Issues

### CORS error: `blocked by CORS policy`

```csharp
// Fix: add frontend origin to AllowedOrigins in appsettings
"AllowedOrigins": "http://localhost:3000,https://edurag.yourdomain.com"
```

### JWT token not sent with requests

Check: Axios interceptor must be set up before any request is made.

```typescript
// src/shared/api/axiosInstance.ts
axios.interceptors.request.use(config => {
  const token = useAuthStore.getState().token;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});
```

---

## Related Docs

- [[05-Deployment]] — setup steps
- [[04-Configuration]] — configuration values
- [[../Architecture/07-AI-Pipeline]] — Ollama integration details
