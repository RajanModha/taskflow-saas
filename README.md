# TaskFlow

Full-stack workspace for **TaskFlow**: a SaaS-style task management app. The backend is **ASP.NET Core** on **.NET 10** with a small **clean architecture** layout; the frontend is **React** with **Vite** and **TypeScript**.

## Repository layout

| Path | Description |
|------|-------------|
| `backend/` | .NET solution (`TaskFlow.slnx`) and projects |
| `backend/TaskFlow.Domain` | Domain entities and core model |
| `backend/TaskFlow.Application` | Application abstractions and DI registration |
| `backend/TaskFlow.Infrastructure` | Infrastructure services (implements application contracts) |
| `backend/TaskFlow.API` | ASP.NET Core Web API host |
| `frontend/` | Vite + React + TypeScript SPA |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (solution targets `net10.0`)
- [Node.js](https://nodejs.org/) (LTS recommended) and npm, for the frontend

HTTPS dev URLs use the ASP.NET dev certificate. If the browser warns about HTTPS, run:

```powershell
dotnet dev-certs https --trust
```

## Run the backend

From the repository root:

```powershell
cd backend
dotnet run --project TaskFlow.API
```

By default (see `TaskFlow.API/Properties/launchSettings.json`):

- **HTTPS:** `https://localhost:7043`
- **HTTP:** `http://localhost:5005`

Example API route (uses Application → Infrastructure wiring): `GET /api/info` → JSON with the application name.

Optional OpenAPI document in Development: see `Program.cs` (`MapOpenApi`).

## Run the frontend

Install dependencies once, then start the dev server:

```powershell
cd frontend
npm install
npm run dev
```

The dev server listens on **http://localhost:5173** (configured in `frontend/vite.config.ts`). The API enables CORS for `http://localhost:5173` and `https://localhost:5173` so you can call the backend from the browser while both run.

Production build:

```powershell
cd frontend
npm run build
npm run preview
```

## Run apps independently

You do **not** need both running at once. Start **only** the API for backend work, or **only** Vite for UI work. Start both when you want the SPA to talk to the API from the browser.

## Run with Docker (one command)

TaskFlow can run fully containerized (frontend + backend + PostgreSQL) via Docker Compose.

### 1) Create env file

From repo root:

```powershell
copy .env.example .env
```

Update `.env` values as needed, especially `JWT_SIGNING_KEY`.

### 2) Start everything

From repo root:

```powershell
docker compose up --build
```

That one command builds and starts:
- `postgres` (PostgreSQL)
- `backend` (ASP.NET Core API)
- `frontend` (Nginx serving React build)

### URLs

- Frontend: `http://localhost:5173`
- Backend API (direct): `http://localhost:5005`
- Backend API via frontend proxy: `http://localhost:5173/api`

### Stop

```powershell
docker compose down
```

To also remove DB volume data:

```powershell
docker compose down -v
```
