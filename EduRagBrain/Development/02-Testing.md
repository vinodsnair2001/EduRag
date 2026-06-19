---
tags: [development, testing, postman, integration-tests]
created: 2026-06-18
updated: 2026-06-18
type: development
status: stable
aliases: [Testing, Integration Tests, Postman]
---

# Testing

> [[_HOME|← Home]] · [[00-Build-Order|← Build Order]]

## Test Strategy

| Layer | Method | What to verify |
|-------|--------|---------------|
| Auth | HTTP / Postman | JWT returned, role in token, 401 on bad creds |
| CRUD | HTTP / Postman | Create, read, update, delete for all entities |
| Upload | HTTP / Postman | PDF accepted, non-PDF rejected, duplicate rejected |
| Vectorization | DB query | `MaterialChunks` populated, status = Completed |
| RAG Chat | HTTP / Postman + SSE | Tokens stream, answer from context, chunks saved |
| Security | HTTP | Unauthorised 401, wrong role 403, wrong session 403 |
| Frontend | Browser | End-to-end happy path, loading states, error states |

---

## Phase 2: Auth + CRUD (Postman)

### Login

```http
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{ "email": "admin@edurag.com", "password": "Admin@123" }
```

Expected: `200 { "token": "eyJ...", "role": "Admin" }`

### Create Class

```http
POST http://localhost:5000/api/admin/classes
Authorization: Bearer {{token}}
Content-Type: application/json

{ "name": "Class 7", "grade": 7 }
```

Expected: `201 { "id": 1, "name": "Class 7", "grade": 7 }`

### Create Subject

```http
POST http://localhost:5000/api/admin/classes/1/subjects
Authorization: Bearer {{token}}
Content-Type: application/json

{ "name": "Science", "description": "Physics, Chemistry, Biology" }
```

### Create Chapter

```http
POST http://localhost:5000/api/admin/subjects/1/chapters
Authorization: Bearer {{token}}
Content-Type: application/json

{ "title": "Chapter 1 — Photosynthesis", "orderIndex": 1 }
```

---

## Phase 4: Upload + Vectorization

### Upload PDF

```http
POST http://localhost:5000/api/admin/upload
Authorization: Bearer {{token}}
Content-Type: multipart/form-data

file: <attach test.pdf>
classId: 1
subjectId: 1
chapterId: 1
```

Expected: `202 { "materialId": "...", "status": "Pending" }`

### Verify Vectorization

```sql
-- Run in psql or DB tool
SELECT "Id", "OriginalFileName", "VectorizationStatus"
FROM "StudyMaterials"
ORDER BY "UploadedAt" DESC;

SELECT COUNT(*) FROM "MaterialChunks" WHERE "SubjectId" = 1;
```

Expected: status = 2 (Completed), MaterialChunks has rows.

---

## Phase 5: RAG Chat

### Create Student + Login

```http
POST http://localhost:5000/api/auth/register
Authorization: Bearer {{admin_token}}
Content-Type: application/json

{ "email": "student@test.com", "fullName": "Test Student",
  "password": "Student@123", "role": 1 }
```

```http
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{ "email": "student@test.com", "password": "Student@123" }
```

### Create Chat Session

```http
POST http://localhost:5000/api/chat/sessions
Authorization: Bearer {{student_token}}
Content-Type: application/json

{ "classId": 1, "subjectId": 1 }
```

Expected: `201 { "sessionId": "..." }`

### Send Chat Message (SSE)

Use `curl` to see raw SSE stream:

```bash
curl -X POST "http://localhost:5000/api/chat/sessions/{sessionId}/messages" \
  -H "Authorization: Bearer {student_token}" \
  -H "Content-Type: application/json" \
  -d '{"content": "What is photosynthesis?"}' \
  --no-buffer
```

Expected: stream of `data: {token}\n\n` lines, ending with `data: [DONE]`.

### Verify Source Chunks Saved

```sql
SELECT "Content", "Role", "SourceChunkIds"
FROM "ChatMessages"
WHERE "SessionId" = '{session_id}'
ORDER BY "SentAt";
```

Expected: assistant message with non-null `SourceChunkIds` JSON array.

---

## Security Tests

### Test Unauthorized Access

```http
GET http://localhost:5000/api/admin/classes
# No Authorization header
```

Expected: `401 Unauthorized`

### Test Wrong Role

```http
GET http://localhost:5000/api/admin/classes
Authorization: Bearer {{student_token}}
```

Expected: `403 Forbidden`

### Test Session Ownership Enforcement

```http
# Create a session as student1, get sessionId
# Login as student2
# Try to send message to student1's session
POST http://localhost:5000/api/chat/sessions/{student1_session_id}/messages
Authorization: Bearer {{student2_token}}
```

Expected: `403 Forbidden`

---

## Integration Test Scenarios

| # | Scenario | Steps | Expected |
|---|---------|-------|----------|
| 1 | Full upload → chat pipeline | Upload PDF → wait → ask question | Answer from PDF content |
| 2 | Duplicate PDF rejection | Upload same PDF twice | Second upload returns 409 |
| 3 | Delete material → chat returns no context | Upload → chat (works) → delete → chat again | "I couldn't find this..." |
| 4 | Wrong file type rejection | Upload .docx file | 400 Bad Request |
| 5 | Practice question generation | Send "Quiz me" in chat | 5 structured questions |
| 6 | Answer verification | Submit answer to practice question | CORRECT/INCORRECT/PARTIAL verdict |

---

## Related Docs

- [[00-Build-Order]] — when to run each test phase
- [[../System/02-API-Reference]] — full endpoint details
- [[../System/06-Troubleshooting]] — when tests fail
