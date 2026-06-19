---
tags: [system, security, jwt, auth, bcrypt, cors]
created: 2026-06-18
updated: 2026-06-18
type: system
status: stable
aliases: [Security, Auth, JWT]
---

# Security

> [[_HOME|← Home]] · [[00-System-Overview|← System Overview]]

## Authentication

### JWT Bearer Tokens

- Algorithm: `HS256`
- Secret: minimum 256-bit random key, stored in environment variable (never in source)
- Access token expiry: **8 hours**
- Refresh token expiry: **7 days** (stored in `httpOnly` cookie — not accessible via JS)
- `ClockSkew: TimeSpan.Zero` — no tolerance on expiry

```csharp
// Token validation parameters
new TokenValidationParameters {
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
    ValidateIssuer   = true,  ValidIssuer   = "EduRAG",
    ValidateAudience = true,  ValidAudience = "EduRAG",
    ClockSkew = TimeSpan.Zero
}
```

### JWT Claims

| Claim | Value |
|-------|-------|
| `sub` | User UUID |
| `role` | `Admin` or `Student` |
| `name` | Full name |
| `exp` | Expiry timestamp |

---

## Password Storage

- Library: **BCrypt.Net-Next**
- Work factor: **11** (≈300ms on modern hardware — balanced security/UX)
- Never stored in plaintext; never logged

```csharp
// Hash on register
string hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);

// Verify on login
bool valid = BCrypt.Net.BCrypt.Verify(password, storedHash);
```

---

## Role-Based Authorization

Two roles: `Admin` (0) and `Student` (1).

```csharp
// Admin-only endpoints
[Authorize(Roles = "Admin")]

// Student-only endpoints
[Authorize(Roles = "Student")]

// Public endpoint
[AllowAnonymous]
```

No endpoint is accessible without a valid JWT (except `/api/auth/login`). Role claims are validated at the controller level by the ASP.NET Core middleware — no manual role checks in use-cases.

---

## File Upload Safety

| Check | Implementation |
|-------|--------------|
| Extension check | Must end with `.pdf` |
| MIME type check | Must be `application/pdf` |
| Size limit | ≤ 50 MB (`[RequestSizeLimit(52_428_800)]`) |
| Rename on disk | Files stored as `{guid}.pdf` — original name is only in the DB |
| Path traversal | `IFileStorageService` constructs path from integer IDs — no user-supplied path segments |
| Duplicate detection | SHA-256 `ContentHash` — identical files rejected with 409 |

---

## SQL Injection Prevention

- **Dapper**: all queries use `@param` parameterized SQL — no string concatenation
- **EF Core**: LINQ-to-SQL is parameterized by design
- **pgvector query**: vector literal is formatted as a string (`'[0.1,0.2,...]'`) — this is safe because it is constructed from `float[]` values, not user input

---

## Chat Scope Isolation

Students cannot see or affect other students' data:

1. `ChatSession.UserId` is set to the authenticated user's ID on creation
2. On every message, the API verifies `session.UserId == authenticatedUserId`
3. Vector search **always** filters by `ClassId + SubjectId` from the session record — a student cannot escape their subject scope by crafting a query
4. `SourceChunkIds` stored in messages are UUIDs — cannot be used to retrieve content via any exposed endpoint

---

## CORS Policy

```csharp
// Development: allow all (for localhost dev)
// Production: restrict to frontend origin only
builder.Services.AddCors(o => o.AddPolicy("FrontendPolicy", p =>
    p.WithOrigins(config["AllowedOrigins"]!.Split(','))
     .AllowAnyMethod()
     .AllowAnyHeader()
     .AllowCredentials()   // needed for httpOnly refresh token cookie
));
```

**Never use `AllowAnyOrigin()` in production.**

---

## HTTPS

- Development: ASP.NET Core dev cert (`dotnet dev-certs https --trust`)
- Production: Let's Encrypt via nginx reverse proxy, or self-signed cert
- Redirect HTTP → HTTPS enforced via `app.UseHttpsRedirection()`

---

## Rate Limiting

Applied on chat endpoints to prevent abuse:

```csharp
// Fixed window: 30 requests per user per minute on /api/chat
builder.Services.AddRateLimiter(o => o.AddFixedWindowLimiter("chat", opt => {
    opt.PermitLimit = 30;
    opt.Window = TimeSpan.FromMinutes(1);
    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    opt.QueueLimit = 5;
}));
```

---

## Security Checklist

| Item | Status |
|------|--------|
| Passwords BCrypt-hashed | ✓ |
| JWT secret in env var | ✓ |
| Short-lived access tokens | ✓ |
| httpOnly refresh cookie | ✓ |
| Role enforcement on all admin routes | ✓ |
| File type + size validation | ✓ |
| Files renamed on disk | ✓ |
| Parameterized SQL everywhere | ✓ |
| CORS restricted to frontend origin | ✓ |
| Chat session ownership verified | ✓ |
| Vector search scoped to class+subject | ✓ |
| Rate limiting on chat | ✓ |
| HTTPS in production | ✓ |

---

## Related Docs

- [[02-API-Reference]] — endpoint auth requirements
- [[04-Configuration]] — JWT secret and CORS configuration
- [[../Architecture/05-API-Layer]] — middleware registration
