# EduRAG — How to Run the Project

## Default Credentials

| Role | Email | Password |
|------|-------|----------|
| **Admin** | `admin@edurag.local` | `Admin@123` |
| **Student** | Created by admin via User Management | Set by admin |

> **Change the admin password immediately after first login.**

---

## Local Dev Environment — What Runs Where

| Service | How | Host | Port |
|---------|-----|------|------|
| PostgreSQL | **Local install** (not Docker) | localhost | **5433** |
| Ollama (AI) | **Local Windows app** (not Docker) | localhost | 11434 |
| ASP.NET Core API | `dotnet watch run` | localhost | **5000** |
| React Frontend | `npm run dev` (Vite) | localhost | **5173** |

**Docker is not used for local dev.** Both PostgreSQL and Ollama run as local Windows services.

**File paths on this machine:**
| Purpose | Path |
|---------|------|
| Ollama models | `C:\Users\vinod.kumar\AppData\Local\Programs\Ollama` (managed by Ollama app) |
| Uploaded PDFs | `E:\EduRagFiles\EduRagPdfs` |

---

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | 8.x | `dotnet --version` to check |
| Node.js | 20.x LTS | `node --version` to check |
| Docker Desktop | 4.x+ | Must be running before starting Ollama |
| PostgreSQL | 16 | Local install on port 5433 with pgvector extension |
| `dotnet-ef` CLI | latest | One-time install (see below) |

```powershell
# Install EF Core tools (once per machine)
dotnet tool install --global dotnet-ef
```

---

## First-Time Setup

### 1. Verify Local PostgreSQL

```powershell
# Confirm PostgreSQL is running on port 5433
psql -h localhost -p 5433 -U postgres -c "SELECT version();"

# Confirm pgvector extension is available
psql -h localhost -p 5433 -U postgres -c "SELECT * FROM pg_available_extensions WHERE name = 'vector';"
```

If pgvector is missing, install it: https://github.com/pgvector/pgvector

### 2. Create the Database

```powershell
psql -h localhost -p 5433 -U postgres -c "CREATE DATABASE \"Edurag\";"
```

### 3. Create Required Folders

```powershell
New-Item -ItemType Directory -Force -Path "E:\EduRagFiles\Ollama"
New-Item -ItemType Directory -Force -Path "E:\EduRagFiles\EduRagPdfs"
```

### 4. Verify Ollama Is Running

Ollama runs as a local Windows app. It starts automatically with Windows. Verify:

```powershell
ollama list
```

Expected output (both models must appear):
```
NAME                       ID              SIZE
llama3.2:latest            a80c4f17acd5    2.0 GB
nomic-embed-text:latest    0a109f422b47    274 MB
```

If Ollama is not running, start it from the system tray or:
```powershell
ollama serve
```

If models are missing, pull them once:
```powershell
ollama pull nomic-embed-text
ollama pull llama3.2
```

### 6. Run EF Core Migrations

```powershell
cd src\EduRAG.API

# Create the initial migration (only if Migrations folder doesn't exist yet)
dotnet ef migrations add InitialCreate `
  --project ..\EduRAG.Infrastructure `
  --startup-project .

# Apply migrations to the database
dotnet ef database update `
  --project ..\EduRAG.Infrastructure `
  --startup-project .
```

### 7. Seed the Admin User

No manual seeding needed. `Program.cs` automatically inserts the admin on first startup if no admin exists:

```
Email:    admin@edurag.local
Password: Admin@123
Role:     Admin
```

Just start the API (`dotnet watch run`) and the seed runs automatically.

### 8. Frontend First-Time Install

```powershell
cd frontend
npm install
```

Confirm `.env.local` exists and contains:
```
VITE_API_BASE_URL=http://localhost:5000/api
```

---

## Running in Development (Day-to-Day)

Run all three steps. Keep all terminals open.

---

### Step 1 — Confirm Ollama Is Running

Ollama is a local Windows app. Check it's up:

```powershell
ollama list
# Should show: llama3.2 and nomic-embed-text
```

If nothing shows, start it:
```powershell
ollama serve
```

No Docker needed. No terminal to keep open — it runs in the background.

---

### Step 2 — Start the API (Terminal 1)

```powershell
cd src\EduRAG.API
dotnet watch run
```

Wait for:
```
Now listening on: http://localhost:5000
Application started. Press Ctrl+C to shut down.
```

| URL | Purpose |
|-----|---------|
| `http://localhost:5000` | API root |
| `http://localhost:5000/swagger` | Swagger UI — test all endpoints here |

`dotnet watch` hot-reloads on C# file changes.

---

### Step 3 — Start the Frontend (Terminal 2)

```powershell
cd frontend
npm run dev
```

Wait for:
```
  VITE v5.x.x  ready in xxx ms

  ➜  Local:   http://localhost:5173/
```

| URL | Purpose |
|-----|---------|
| `http://localhost:5173` | App (student + admin UI) |
| `http://localhost:5173/admin` | Admin portal |
| `http://localhost:5173/student` | Student portal |

Vite hot-reloads on TypeScript/TSX changes.

---

### Verify Everything Is Running

```powershell
# PostgreSQL — local, should return version string
psql -h localhost -p 5433 -U postgres -d Edurag -c "SELECT version();"

# Ollama — should list both models
ollama list

# API Swagger reachable (expect 200)
curl -s -o NUL -w "%{http_code}" http://localhost:5000/swagger/index.html
```

---

### PowerShell Quick-Start Script

Save as `dev-start.ps1` in the project root, run with `.\dev-start.ps1`:

```powershell
# Confirm Ollama is running (local Windows app)
$ollamaRunning = Get-Process -Name "ollama" -ErrorAction SilentlyContinue
if (-not $ollamaRunning) {
    Write-Host "Starting Ollama..." -ForegroundColor Yellow
    Start-Process ollama -ArgumentList "serve" -WindowStyle Hidden
    Start-Sleep -Seconds 3
}

# Start API in a new window
Start-Process powershell -ArgumentList '-NoExit', '-Command', 'Set-Location src\EduRAG.API; dotnet watch run'

# Start frontend in a new window
Start-Process powershell -ArgumentList '-NoExit', '-Command', 'Set-Location frontend; npm run dev'

Write-Host ""
Write-Host "Ollama   -> http://localhost:11434 (local app)" -ForegroundColor Cyan
Write-Host "API      -> http://localhost:5000/swagger" -ForegroundColor Green
Write-Host "Frontend -> http://localhost:5173" -ForegroundColor Green
```

---

### Stopping Development Servers

```powershell
# Stop API:      Ctrl+C in its terminal
# Stop Frontend: Ctrl+C in its terminal
# Ollama:        runs as a background Windows app — leave it running
# PostgreSQL:    local Windows service — leave it running
```

No Docker to stop for local dev.

---

## Running with Full Docker (All Services Containerised)

This builds and runs everything in containers. PostgreSQL runs inside Docker (separate from your local install).

```powershell
docker-compose build
docker-compose up -d
docker-compose ps   # all four should show "running"
```

| Service | URL |
|---------|-----|
| Frontend | http://localhost:3000 |
| API | http://localhost:5000 |
| PostgreSQL (container) | localhost:5432 |
| Ollama | http://localhost:11434 |

> Note: the containerised postgres runs on port **5432**, not 5433. The API container connects to it via the internal Docker network.

---

## Admin First-Run Checklist

After logging in as admin for the first time:

- [ ] Change the default admin password
- [ ] Create a Class (e.g., "Class 7")
- [ ] Create a Subject under that class (e.g., "Science")
- [ ] Optionally create Chapters
- [ ] Upload a PDF study material
- [ ] Wait for vectorization status → **Completed** (1–5 min)
- [ ] Create a Student account and share credentials

---

## Connection String Reference

| Context | Value |
|---------|-------|
| Local dev | `Host=localhost;Port=5433;Database=Edurag;Username=postgres;Password=<your-db-password>` |
| Docker (container-to-container) | `Host=postgres;Port=5432;Database=edurag;Username=postgres;Password=your_password` |

---

## Ports at a Glance

| Port | Service | How |
|------|---------|-----|
| 5173 | React frontend (Vite dev) | `npm run dev` |
| 5000 | ASP.NET Core API | `dotnet watch run` |
| 5433 | PostgreSQL | Local install |
| 11434 | Ollama | Docker |

---

## Backup & Restore

```powershell
# Backup
pg_dump -h localhost -p 5433 -U postgres Edurag > backup.sql

# Restore
psql -h localhost -p 5433 -U postgres Edurag < backup.sql
```

---

## Troubleshooting

See [EduRagBrain/System/06-Troubleshooting.md](EduRagBrain/System/06-Troubleshooting.md) for common errors and fixes.
