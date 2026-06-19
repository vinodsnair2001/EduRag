---
tags: [system, database, postgresql, pgvector, schema, ddl]
created: 2026-06-18
updated: 2026-06-19
type: system
status: stable
aliases: [Database Schema, DDL, PostgreSQL]
---

# Database Schema

> [[_HOME|← Home]] · [[00-System-Overview|← System Overview]]

## Prerequisites

```sql
-- Run once on the PostgreSQL server
CREATE EXTENSION IF NOT EXISTS vector;     -- pgvector
CREATE EXTENSION IF NOT EXISTS pgcrypto;   -- gen_random_uuid()
```

---

## Tables

### Classes

```sql
CREATE TABLE "Classes" (
    "Id"        SERIAL PRIMARY KEY,
    "Name"      VARCHAR(100) NOT NULL,
    "Grade"     INT NOT NULL CHECK ("Grade" BETWEEN 1 AND 12),
    "IsActive"  BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Subjects

```sql
CREATE TABLE "Subjects" (
    "Id"          SERIAL PRIMARY KEY,
    "Name"        VARCHAR(150) NOT NULL,
    "Description" TEXT NOT NULL DEFAULT '',
    "ClassId"     INT NOT NULL REFERENCES "Classes"("Id") ON DELETE CASCADE,
    "IsActive"    BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt"   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Chapters

```sql
CREATE TABLE "Chapters" (
    "Id"         SERIAL PRIMARY KEY,
    "Title"      VARCHAR(200) NOT NULL,
    "OrderIndex" INT NOT NULL DEFAULT 0,
    "SubjectId"  INT NOT NULL REFERENCES "Subjects"("Id") ON DELETE CASCADE,
    "IsActive"   BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt"  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### AppUsers

```sql
CREATE TABLE "AppUsers" (
    "Id"           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Email"        VARCHAR(255) NOT NULL UNIQUE,
    "FullName"     VARCHAR(200) NOT NULL,
    "PasswordHash" TEXT NOT NULL,           -- BCrypt, work factor 11
    "Role"         INT NOT NULL,            -- 0=Admin, 1=Student
    "ClassId"      INT NULL REFERENCES "Classes"("Id") ON DELETE RESTRICT,
    -- NULL for Admin users; required for Students
    "IsActive"     BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt"    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "LastLoginAt"  TIMESTAMPTZ NULL
);
```

### StudentPermissions

```sql
CREATE TABLE "StudentPermissions" (
    "Id"        UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "StudentId" UUID NOT NULL REFERENCES "AppUsers"("Id") ON DELETE CASCADE,
    "SubjectId" INT  NOT NULL REFERENCES "Subjects"("Id")  ON DELETE CASCADE,
    "GrantedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_student_subject UNIQUE ("StudentId", "SubjectId")
);

CREATE INDEX idx_student_permissions_student ON "StudentPermissions" ("StudentId");
```

> Setting permissions replaces the full row-set for a student (DELETE + INSERT in a single transaction).
> The unique constraint prevents duplicate grants.

### StudyMaterials

```sql
CREATE TABLE "StudyMaterials" (
    "Id"                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "OriginalFileName"    VARCHAR(500) NOT NULL,
    "StoredFilePath"      TEXT NOT NULL,
    "ContentHash"         CHAR(64) NOT NULL,         -- SHA-256 hex, used for dedup
    "FileSizeBytes"       BIGINT NOT NULL DEFAULT 0,
    "ClassId"             INT NOT NULL REFERENCES "Classes"("Id"),
    "SubjectId"           INT NOT NULL REFERENCES "Subjects"("Id"),
    "ChapterId"           INT NULL REFERENCES "Chapters"("Id"),
    "UploadedById"        UUID NOT NULL REFERENCES "AppUsers"("Id"),
    "UploadedAt"          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "VectorizationStatus" INT NOT NULL DEFAULT 0,
    --   0=Pending  1=Processing  2=Completed  3=Failed
    "VectorizationError"  TEXT NULL
);
```

### MaterialChunks — vector store

```sql
CREATE TABLE "MaterialChunks" (
    "Id"         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "MaterialId" UUID NOT NULL REFERENCES "StudyMaterials"("Id") ON DELETE CASCADE,
    "ClassId"    INT NOT NULL,       -- denormalized for fast filtered search
    "SubjectId"  INT NOT NULL,
    "ChapterId"  INT NULL,
    "Content"    TEXT NOT NULL,
    "ChunkIndex" INT NOT NULL,
    "PageNumber" INT NOT NULL DEFAULT 1,
    "Embedding"  vector(768) NOT NULL  -- nomic-embed-text output
);

-- HNSW index for approximate nearest-neighbour search
CREATE INDEX idx_chunks_embedding
    ON "MaterialChunks" USING hnsw ("Embedding" vector_cosine_ops)
    WITH (m = 16, ef_construction = 64);

-- Composite index: pre-filter by class+subject before vector search
CREATE INDEX idx_chunks_class_subject ON "MaterialChunks" ("ClassId", "SubjectId");
```

### ChatSessions

```sql
CREATE TABLE "ChatSessions" (
    "Id"        UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId"    UUID NOT NULL REFERENCES "AppUsers"("Id"),
    "ClassId"   INT NOT NULL REFERENCES "Classes"("Id"),
    "SubjectId" INT NOT NULL REFERENCES "Subjects"("Id"),
    "StartedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "EndedAt"   TIMESTAMPTZ NULL
);
```

### ChatMessages

```sql
CREATE TABLE "ChatMessages" (
    "Id"             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "SessionId"      UUID NOT NULL REFERENCES "ChatSessions"("Id") ON DELETE CASCADE,
    "Content"        TEXT NOT NULL,
    "Role"           INT NOT NULL,      -- 0=User, 1=Assistant
    "SentAt"         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "SourceChunkIds" TEXT NULL          -- JSON array of chunk UUIDs used for RAG
);
```

---

## Seed Data

```sql
-- Default admin account (password: Admin@123)
-- Replace BCrypt hash with actual generated hash before first run
INSERT INTO "AppUsers" ("Email", "FullName", "PasswordHash", "Role")
VALUES (
    'admin@edurag.com',
    'System Admin',
    '$2a$11$REPLACE_WITH_ACTUAL_BCRYPT_HASH',
    0   -- Admin
);
```

> Generate hash: `BCrypt.Net.BCrypt.HashPassword("Admin@123", 11)`

---

## Entity Relationship Diagram

```
Classes ──(1:N)── Subjects ──(1:N)── Chapters
    │                  │                  │
    │ (AppUsers.ClassId)└──(1:N)── StudyMaterials ─┘
    │                                      │
AppUsers ──(1:N)── StudentPermissions ──► Subjects
    │                               (1:N cascade on delete)
    └──(1:N)── ChatSessions ──(1:N)── ChatMessages
                                      MaterialChunks
                                       (vector(768))
```

---

## Performance Notes

| Concern | Solution |
|---------|---------|
| Vector search speed | HNSW index with `m=16, ef_construction=64` |
| Filtered vector search | Composite index on `(ClassId, SubjectId)` pre-filters rows |
| Read query performance | Dapper (no change tracking) for all SELECT |
| Write performance | EF Core only for INSERT/UPDATE/DELETE |
| Duplicate PDF uploads | SHA-256 `ContentHash` unique guard in application layer |

---

## Related Docs

- [[../Architecture/02-Domain-Layer]] — C# entity definitions
- [[../Architecture/04-Infrastructure-Layer]] — EF Core configurations
- [[../Architecture/07-AI-Pipeline]] — MaterialChunks usage in vector search
