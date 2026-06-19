---
tags: [system, api, endpoints, rest, http]
created: 2026-06-18
updated: 2026-06-18
type: system
status: stable
aliases: [API Reference, Endpoints]
---

# API Reference

> [[_HOME|← Home]] · [[00-System-Overview|← System Overview]]

Base URL: `http://localhost:5000/api`
Auth header: `Authorization: Bearer <jwt_token>`

---

## Auth Endpoints

### POST `/auth/login`

Login and receive JWT.

**Request:**
```json
{ "email": "admin@edurag.com", "password": "Admin@123" }
```

**Response 200:**
```json
{
  "token":    "eyJhbGci...",
  "role":     "Admin",
  "fullName": "System Admin",
  "userId":   "3fa85f64-..."
}
```

**Errors:** 401 invalid credentials, 400 validation

---

### POST `/auth/register`  `[Admin]`

Create a new Admin or Student account.

**Request:**
```json
{ "email": "student@school.com", "fullName": "Alice Smith", "password": "Pass@123", "role": 1 }
```

**Response 201:**
```json
{ "userId": "...", "email": "...", "role": "Student" }
```

---

## Admin — Classes

### GET `/admin/classes`  `[Admin]`

Returns all active classes with their subjects.

**Response 200:**
```json
[
  {
    "id": 1, "name": "Class 6", "grade": 6,
    "subjects": [
      { "id": 10, "name": "Mathematics", "classId": 1 }
    ]
  }
]
```

### POST `/admin/classes`  `[Admin]`

```json
// Request
{ "name": "Class 7", "grade": 7 }

// Response 201
{ "id": 2, "name": "Class 7", "grade": 7, "isActive": true }
```

### PUT `/admin/classes/{id}`  `[Admin]`

```json
{ "name": "Class 7 Advanced", "grade": 7, "isActive": true }
```

### DELETE `/admin/classes/{id}`  `[Admin]`

Cascades: deletes all subjects → chapters → study materials → material chunks.

---

## Admin — Subjects

### GET `/admin/classes/{classId}/subjects`  `[Admin]`

### POST `/admin/classes/{classId}/subjects`  `[Admin]`

```json
{ "name": "Science", "description": "Physics, Chemistry, Biology" }
```

### PUT `/admin/subjects/{id}`  `[Admin]`

### DELETE `/admin/subjects/{id}`  `[Admin]`

---

## Admin — Chapters

### GET `/admin/subjects/{subjectId}/chapters`  `[Admin]`

Returns chapters ordered by `OrderIndex`.

### POST `/admin/subjects/{subjectId}/chapters`  `[Admin]`

```json
{ "title": "Chapter 1 — Introduction", "orderIndex": 1 }
```

### PUT `/admin/chapters/{id}`  `[Admin]`

### DELETE `/admin/chapters/{id}`  `[Admin]`

---

## Admin — Study Materials

### POST `/admin/upload`  `[Admin]`

```
Content-Type: multipart/form-data
Max size: 50 MB
```

**Form fields:**

| Field | Type | Required |
|-------|------|----------|
| `file` | PDF file | Yes |
| `classId` | int | Yes |
| `subjectId` | int | Yes |
| `chapterId` | int | No |

**Response 202 (accepted for vectorization):**
```json
{
  "materialId": "3fa85f64-...",
  "status":     "Pending"
}
```

**Errors:** 400 not a PDF, 400 too large, 409 duplicate file (same SHA-256 hash)

### GET `/admin/materials`  `[Admin]`

Query params: `classId`, `subjectId`, `status` (optional filters)

```json
[
  {
    "id": "...", "originalFileName": "ch1.pdf",
    "fileSizeBytes": 204800, "status": "Completed",
    "uploadedAt": "2026-06-18T10:00:00Z"
  }
]
```

### DELETE `/admin/materials/{id}`  `[Admin]`

Deletes material record, all chunks, and the stored file.

---

## Student — Selection

### GET `/student/classes`  `[Student]`

Same shape as admin classes endpoint. Used for class selection screen.

### GET `/student/classes/{classId}/subjects`  `[Student]`

### GET `/student/subjects/{subjectId}/chapters`  `[Student]`

---

## Chat

### POST `/chat/sessions`  `[Student]`

Start a new chat session for a class+subject.

```json
// Request
{ "classId": 1, "subjectId": 10 }

// Response 201
{ "sessionId": "3fa85f64-..." }
```

### GET `/chat/sessions/{sessionId}/messages`  `[Student]`

Load message history (max 50 most recent).

```json
[
  { "id": "...", "content": "What is photosynthesis?", "role": "User",      "sentAt": "..." },
  { "id": "...", "content": "Photosynthesis is...",    "role": "Assistant", "sentAt": "..." }
]
```

### POST `/chat/sessions/{sessionId}/messages`  `[Student]`

**The main streaming endpoint.** Sends a message; response is an SSE stream.

```json
// Request body
{ "content": "Explain the water cycle" }
```

**Response headers:**
```
Content-Type: text/event-stream
Cache-Control: no-cache
```

**Response body (stream):**
```
data: The

data:  water

data:  cycle

data:  is...

data: [DONE]
```

**Errors:** 403 session does not belong to this user, 404 session not found

---

## Common Error Responses

```json
// 400 Bad Request
{ "statusCode": 400, "message": "Validation failed", "errors": { "email": ["Required"] } }

// 401 Unauthorized
{ "statusCode": 401, "message": "Token expired or invalid" }

// 403 Forbidden
{ "statusCode": 403, "message": "Access denied for this role" }

// 404 Not Found
{ "statusCode": 404, "message": "Resource not found" }

// 409 Conflict
{ "statusCode": 409, "message": "File with identical content already exists" }

// 500 Internal Server Error
{ "statusCode": 500, "message": "An unexpected error occurred", "traceId": "..." }
```

---

## Related Docs

- [[../Architecture/05-API-Layer]] — controller implementation
- [[03-Security]] — auth and JWT details
- [[../Development/02-Testing]] — Postman test scenarios
