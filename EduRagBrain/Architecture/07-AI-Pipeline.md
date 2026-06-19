---
tags: [architecture, ai, rag, ollama, llama3, embeddings, pgvector]
created: 2026-06-18
updated: 2026-06-18
type: architecture
status: stable
aliases: [AI Pipeline, RAG, Ollama, Vectorization]
---

# AI Pipeline

> [[_HOME|← Home]] · [[00-Overview|← Overview]]

## Key Principle

**All AI runs locally via Ollama. Zero paid API calls. Zero token costs.**

| Model | Purpose | Dimensions | Command |
|-------|---------|-----------|---------|
| `nomic-embed-text` | Text → vector embedding | 768 | `ollama pull nomic-embed-text` |
| `llama3.2` | Chat completion (8B params) | — | `ollama pull llama3.2` |

Ollama serves both at `http://localhost:11434`.

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
     └── OllamaAIService.GetEmbeddingAsync(chunk.Text)
         → POST http://localhost:11434/api/embeddings
         → returns float[768]
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
  3. OllamaAIService.GetEmbeddingAsync(question) → float[768]
  4. VectorSearchService.SearchAsync(embedding, classId, subjectId, topK=5)
     └── Dapper query:
         SELECT Id, Content, PageNumber,
                1 - (Embedding <=> @vector::vector) AS Score
         FROM "MaterialChunks"
         WHERE "ClassId" = @c AND "SubjectId" = @s
         ORDER BY Embedding <=> @vector::vector
         LIMIT 5
  5. Build system prompt (inject top-5 chunks as CONTEXT)
  6. ChatQueries.GetLastNMessages(sessionId, 10) → history
  7. OllamaAIService.StreamChatAsync(prompt, history, question)
     └── POST http://localhost:11434/api/chat  {stream: true}
         → yields string tokens via IAsyncEnumerable<string>
  8. Per token: write "data: {token}\n\n" to HTTP response (SSE)
  9. Accumulate full response
 10. Save assistant ChatMessage + SourceChunkIds (JSON array of chunk GUIDs)
```

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
    return result!.Embedding;
}

// POST /api/chat  (streaming)
public async IAsyncEnumerable<string> StreamChatAsync(
    string systemPrompt, IEnumerable<ChatMessageDto> history, string userMessage,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    // build messages array: system + history + userMessage
    var request = new HttpRequestMessage(HttpMethod.Post, "/api/chat") {
        Content = JsonContent.Create(new { model = "llama3.2", messages, stream = true })
    };
    using var response = await _http.SendAsync(request,
        HttpCompletionOption.ResponseHeadersRead, ct);
    using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(ct));

    while (!reader.EndOfStream && !ct.IsCancellationRequested)
    {
        var line = await reader.ReadLineAsync();
        var chunk = JsonSerializer.Deserialize<ChatStreamChunk>(line);
        if (chunk?.Message?.Content is not null)
            yield return chunk.Message.Content;
        if (chunk?.Done == true) break;
    }
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

### Practice Question Generation

Triggered when message contains: `"practice questions"`, `"quiz me"`, `"test me"`, `"create questions"`.

```
[appended to base prompt]
Generate exactly 5 practice questions from the context above.
Return STRICTLY as JSON:
{
  "questions": [{
    "id": 1,
    "question": "...",
    "type": "short-answer" | "multiple-choice",
    "options": ["A","B","C","D"],
    "correct_answer": "...",
    "explanation": "..."
  }]
}
Difficulty: 2 easy, 2 medium, 1 hard.
Do NOT reveal correct_answer or explanation in the visible response.
```

### Answer Verification

```
[appended to base prompt]
Original question: "{question_text}"
Correct answer:    "{correct_answer}"
Student's answer:  "{student_answer}"

Evaluate:
1. State: CORRECT / PARTIALLY CORRECT / INCORRECT
2. CORRECT → praise + reinforce concept
3. PARTIALLY CORRECT → explain what was right / missing
4. INCORRECT → gently correct with explanation from context
5. End with encouragement for Class {grade} student
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

---

## Chunking Strategy

| Parameter | Value | Rationale |
|-----------|-------|-----------|
| Chunk size | 500 words | Fits in one embedding context window |
| Overlap | 50 words | Preserves sentence context at chunk boundaries |
| Granularity | Page-level then window | Preserves page number metadata |

---

## Related Docs

- [[04-Infrastructure-Layer]] — VectorizationWorker and PdfProcessingService detail
- [[../System/01-Database-Schema]] — MaterialChunks table DDL
- [[../System/06-Troubleshooting]] — Ollama connection errors, embedding dimension mismatches
