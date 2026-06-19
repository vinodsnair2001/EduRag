---
tags: [system, api, endpoints, rest, http]
created: 2026-06-18
updated: 2026-06-19
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

## Admin — Students

### POST `/admin/students`  `[Admin]`

Create a student account with an assigned class. Use this instead of `/auth/register` for students — it enforces class assignment at creation time.

**Request:**
```json
{ "email": "alice@school.com", "fullName": "Alice Smith", "password": "Pass@123", "classId": 1 }
```

**Response 200:**
```json
{ "id": "...", "email": "alice@school.com", "fullName": "Alice Smith", "role": 1, "classId": 1, "isActive": true }
```

**Errors:** 400 email already registered, 400 validation

---

### PUT `/admin/students/{id}`  `[Admin]`

Edit a student's profile. All fields are required except `newPassword` (omit or set to `null` to keep the existing password).

**Request:**
```json
{
  "fullName":    "Alice Smith",
  "email":       "alice@school.com",
  "classId":     2,
  "isActive":    true,
  "newPassword": "NewPass@456"
}
```

**Response 200:** Updated `UserDto` (same shape as create response).

**Errors:** 400 student not found, 400 email already in use by another account

---

### DELETE `/admin/students/{id}`  `[Admin]`

Soft-deactivate a student account (sets `IsActive = false`). All data — chat history, subject permissions, class assignment — is preserved. The student is immediately blocked from logging in.

To reactivate, use `PUT /admin/students/{id}` with `"isActive": true`.

**Response 204** (no content)

**Errors:** 400 student not found, 400 student is already deactivated

---

### GET `/admin/students/{id}/permissions`  `[Admin]`

List all subject permissions for a student.

**Response 200:**
```json
[
  { "id": "...", "studentId": "...", "subjectId": 10, "subjectName": "Mathematics", "grantedAt": "..." },
  { "id": "...", "studentId": "...", "subjectId": 12, "subjectName": "Science",     "grantedAt": "..." }
]
```

---

### PUT `/admin/students/{id}/permissions`  `[Admin]`

Replace all subject permissions for a student (full replace — not partial update).

**Request:**
```json
{ "subjectIds": [10, 12, 15] }
```

**Response 204** (no content)

**Errors:** 400 student not found

---

## Student — My Class & Subjects

### GET `/student/my-class`  `[Student]`

Returns the student's assigned class.

**Response 200:**
```json
{ "classId": 1, "className": "Class 6", "grade": 6 }
```

**Response 404:** No class assigned to this student — admin must assign one.

---

### GET `/student/classes/{classId}/subjects`  `[Student]`

Returns subjects for the given class, filtered by the student's subject permissions.

**Permission logic:**
- If the student has **no permission records** → returns **all active subjects** in the class (open access).
- If the student has **any permission records** → returns only subjects the admin explicitly granted.

**Response 200:**
```json
[
  { "id": 10, "name": "Mathematics", "description": "...", "classId": 1, "isActive": true },
  { "id": 12, "name": "Science",     "description": "...", "classId": 1, "isActive": true }
]
```

---

### GET `/student/subjects/{subjectId}/chapters`  `[Student]`

Returns chapters for a subject, ordered by `OrderIndex`.

---

## Chat

### POST `/chat/sessions`  `[Student]`

Start a new chat session scoped to a class, subject, and one or more chapters.

```json
// Request
{
  "classId":    1,
  "subjectId":  10,
  "chapterIds": [3, 7]   // empty array = no chapter filter (all subject chunks)
}

// Response 201
{ "sessionId": "3fa85f64-..." }
```

`chapterIds` is stored on the session as a JSON array. Every RAG query within the session filters `MaterialChunks` by `ChapterId IN (chapterIds)`. An empty array skips the chapter filter.

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
