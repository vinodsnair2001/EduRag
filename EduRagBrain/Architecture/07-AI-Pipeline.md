---
tags: [architecture, ai, rag, ollama, mistralai, llama3, embeddings, pgvector]
created: 2026-06-18
updated: 2026-06-19
type: architecture
status: stable
aliases: [AI Pipeline, RAG, Ollama, MistralAI, Vectorization]
---

# AI Pipeline

> [[_HOME|← Home]] · [[00-Overview|← Overview]]

## Key Principle

All AI is routed through the `IAIService` interface. The **active provider** is selected by a single config key — no code changes needed to switch.

| Setting | Value | Effect |
|---------|-------|--------|
| `AI:Provider` | `"Ollama"` (default) | Local Ollama — zero cost, fully offline |
| `AI:Provider` | `"MistralAI"` | Mistral cloud API — requires API key |

---

## Provider Comparison

| Property | Ollama | MistralAI |
|----------|--------|-----------|
| Embed model | `nomic-embed-text` | `mistral-embed` |
| Chat model | `llama3.2` | `mistral-large-latest` |
| Embedding dims | **768** | **1024** |
| Cost | Free (local GPU/CPU) | Paid (per token) |
| Requires internet | No | Yes |
| API key | No | `AI:MistralAI:ApiKey` |
| Service class | `OllamaAIService` | `MistralAIService` |

> **Embedding dimensions differ.** Switching providers requires running a SQL migration script and re-vectorizing all PDFs. See [[../Development/01-Setup-Guide#switching-ai-provider|Switching AI Provider]].

---

## Full Data Flow

### Upload → Vectorize

```
Admin uploads PDF (≤50 MB)
        │
        ▼
UploadMaterialUseCase
  1. SHA-256 hash → dedup check
  2. Save file to /storage/materials/{classId}/{subjectId}/{chapterId}/
  3. Insert StudyMaterial (status = Pending)
  4. Channel<Guid>.Writer.WriteAsync(materialId)
        │
        ▼
VectorizationWorker (BackgroundService)
  reads materialId from Channel
        │
        ▼
VectorizationProcessor
  1. Load material record
  2. Set status = Processing
  3. PdfProcessingService.ExtractAndChunk(stream)
     ├── PdfPig opens PDF
     ├── Per page: extract words → join text
     └── Sliding window: 500 words / 50 overlap
  4. For each chunk:
     └── IAIService.GetEmbeddingAsync(chunk.Text)
         ├── Ollama:    POST localhost:11434/api/embeddings  → float[768]
         └── MistralAI: POST api.mistral.ai/v1/embeddings   → float[1024]
  5. Build MaterialChunk entities (with Embedding)
  6. IMaterialChunkRepository.BulkInsertAsync(chunks)
     → INSERT INTO "MaterialChunks" (... "Embedding") VALUES (... @vector::vector)
  7. Set status = Completed
  ── On error: set status = Failed, save error message
```

### Student Chat → RAG Response

```
Student types question
        │
        ▼
ChatUseCase
  1. Verify session ownership
  2. Save user ChatMessage (EF Core)
  3. IAIService.GetEmbeddingAsync(question)
     ├── Ollama:    → float[768]
     └── MistralAI: → float[1024]
  4. VectorSearchService.SearchAsync(embedding, classId, subjectId, topK=5)
     └── Dapper query (dimension read from AI:EmbeddingDimensions config):
         SELECT Id, Content, PageNumber,
                1 - (Embedding <=> @vector::vector({dims})) AS Score
         FROM "MaterialChunks"
         WHERE "ClassId" = @c AND "SubjectId" = @s
         ORDER BY Embedding <=> @vector::vector({dims})
         LIMIT 5
  5. Build system prompt (inject top-5 chunks as CONTEXT)
  6. ChatQueries.GetLastNMessages(sessionId, 10) → history
  7. IAIService.StreamChatAsync(prompt, history, question)
     ├── Ollama:    POST localhost:11434/api/chat     {stream: true}
     └── MistralAI: POST api.mistral.ai/v1/chat/completions {stream: true}
         → yields string tokens via IAsyncEnumerable<string>
  8. Per token: write "data: {token}\n\n" to HTTP response (SSE)
  9. Accumulate full response
 10. Save assistant ChatMessage + SourceChunkIds (JSON array of chunk GUIDs)
```

---

## Service Registration

`ServiceRegistration.cs` selects the provider at startup:

```csharp
var aiProvider = config["AI:Provider"] ?? "Ollama";
if (aiProvider.Equals("MistralAI", StringComparison.OrdinalIgnoreCase))
{
    services.AddHttpClient<IAIService, MistralAIService>(c =>
        c.BaseAddress = new Uri(config["AI:MistralAI:BaseUrl"] ?? "https://api.mistral.ai"));
}
else
{
    services.AddHttpClient<IAIService, OllamaAIService>(c =>
        c.BaseAddress = new Uri(config["Ollama:BaseUrl"] ?? "http://localhost:11434"));
}
```

Both services implement the same `IAIService` interface — all upstream code (`VectorizationProcessor`, `ChatUseCase`) is unaffected.

---

## OllamaAIService — Key Code

```csharp
// POST /api/embeddings
public async Task<float[]> GetEmbeddingAsync(string text)
{
    var response = await _http.PostAsJsonAsync("/api/embeddings",
        new { model = "nomic-embed-text", prompt = text });
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<EmbedResponse>();
    return result!.Embedding;  // float[768]
}

// POST /api/chat  (streaming — Ollama NDJSON format)
public async IAsyncEnumerable<string> StreamChatAsync(...)
{
    // Each line: {"message":{"content":"..."},"done":false}
    // Last line: {"done":true}
}
```

## MistralAIService — Key Code

```csharp
// POST /v1/embeddings  (OpenAI-compatible)
public async Task<float[]> GetEmbeddingAsync(string text)
{
    var response = await _http.PostAsJsonAsync("/v1/embeddings",
        new { model = "mistral-embed", input = new[] { text } });
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<MistralEmbedResponse>();
    return result!.Data[0].Embedding;  // float[1024]
}

// POST /v1/chat/completions  (OpenAI-compatible SSE)
public async IAsyncEnumerable<string> StreamChatAsync(...)
{
    // Each SSE line: data: {"choices":[{"delta":{"content":"..."},"finish_reason":null}]}
    // Final line:    data: [DONE]
}
```

---

## RAG Prompt Templates

### Base (Question Answering)

```
You are an expert tutor for Class {grade}, subject "{subjectName}".
You help students understand their study material deeply and clearly.

RULES:
1. Answer ONLY based on the CONTEXT sections provided below.
2. If not enough info: "I couldn't find this in the uploaded study material."
3. Explain at a level appropriate for Class {grade} students.
4. Use examples and analogies when helpful.
5. Never make up facts not in the context.

CONTEXT FROM STUDY MATERIAL:
--- CHUNK 1 (Page {page}) ---
{chunk1_text}
--- CHUNK 2 (Page {page}) ---
{chunk2_text}
... (up to 5 chunks)
```

---

## pgvector Index

```sql
-- HNSW — approximate nearest neighbour, very fast at search time
CREATE INDEX idx_chunks_embedding
    ON "MaterialChunks" USING hnsw ("Embedding" vector_cosine_ops)
    WITH (m = 16, ef_construction = 64);

-- Composite index for filtered search pre-filter
CREATE INDEX idx_chunks_class_subject ON "MaterialChunks" ("ClassId", "SubjectId");
```

HNSW parameters: `m=16` (graph connectivity), `ef_construction=64` (build quality). Cosine distance operator `<=>` measures angle between vectors — suitable for normalized text embeddings.

> **Note:** After switching providers and running the SQL migration script, rebuild the HNSW index:
> ```sql
> DROP INDEX IF EXISTS idx_chunks_embedding;
> CREATE INDEX idx_chunks_embedding
>     ON "MaterialChunks" USING hnsw ("Embedding" vector_cosine_ops)
>     WITH (m = 16, ef_construction = 64);
> ```

---

## Chunking Strategy

| Parameter | Value | Rationale |
|-----------|-------|-----------|
| Chunk size | 500 words | Fits in one embedding context window |
| Overlap | 50 words | Preserves sentence context at chunk boundaries |
| Granularity | Page-level then window | Preserves page number metadata |

The same `PdfProcessingService` is used regardless of AI provider.

---

## Related Docs

- [[04-Infrastructure-Layer]] — VectorizationWorker and PdfProcessingService detail
- [[../System/01-Database-Schema]] — MaterialChunks table DDL
- [[../System/04-Configuration]] — AI provider config keys
- [[../Development/01-Setup-Guide]] — switching AI provider step-by-step
- [[../System/06-Troubleshooting]] — Ollama connection errors, embedding dimension mismatches
