# TaskFlow

TaskFlow is a multi-tenant, SaaS-style project and task management platform built for portfolio demonstration and real-world architecture practice.  
It includes tenant-safe data isolation, JWT auth with roles, CQRS-based APIs, dashboard analytics, and a modern responsive frontend.

## Project Overview

This repository contains a complete full-stack application:
- `backend/` - ASP.NET Core Web API (.NET 10) with Clean Architecture boundaries
- `frontend/` - React + Vite + TypeScript SPA
- `docker-compose.yml` - one-command local orchestration for app + PostgreSQL

TaskFlow focuses on production-minded fundamentals:
- tenant isolation by organization/workspace
- structured API validation + error handling
- logging and versioned API docs
- clean UX with loading, error, and responsive states

## Features

- Authentication and authorization
  - user registration/login with JWT
  - role support (`Admin`, `User`)
  - profile endpoint with workspace context
- Multi-tenancy
  - workspace create/join flow
  - `org_id` claim enforcement
  - tenant-scoped EF Core query filters and FK constraints
- Project and task management
  - CRUD for projects and tasks
  - filtering, sorting, pagination
  - task status, priority, due-date support
- Dashboard analytics
  - total/completed/pending tasks
  - tasks-by-status aggregation
  - chart-based visualization
- Production readiness
  - Serilog request/application logging
  - global exception handling with Problem Details
  - FluentValidation
  - API versioning + Swagger
- Containerized workflow
  - backend + frontend + PostgreSQL via Docker Compose
  - environment-based configuration

## Tech Stack

- Backend
  - .NET 10, ASP.NET Core Web API
  - EF Core + PostgreSQL
  - ASP.NET Core Identity
  - MediatR (CQRS)
  - AutoMapper
  - FluentValidation
  - Serilog
  - Swagger / OpenAPI + API Versioning
- Frontend
  - React 19, Vite, TypeScript
  - React Router
  - Axios
  - Recharts
  - Tailwind CSS
- DevOps / Tooling
  - Docker, Docker Compose
  - Nginx (frontend serving and API reverse proxy)

## Repository Structure

| Path | Description |
|------|-------------|
| `backend/TaskFlow.Domain` | Domain entities and core rules |
| `backend/TaskFlow.Application` | Contracts, DTOs, validation, mapping |
| `backend/TaskFlow.Infrastructure` | EF Core, Identity, handlers, services |
| `backend/TaskFlow.API` | API host, middleware, controllers, startup |
| `frontend/src` | SPA pages, API clients, auth/context, UI |
| `docker-compose.yml` | Multi-container local environment |

## Setup Instructions

### Option A: Docker (recommended, one command)

1. Create env file from template:

```powershell
copy .env.example .env
```

2. Update `.env` values, especially `JWT_SIGNING_KEY` (32+ chars).

3. Start everything:

```powershell
docker compose up --build
```

Services and URLs:
- Frontend: `http://localhost:5173`
- Backend (direct): `http://localhost:5005`
- Backend via frontend proxy: `http://localhost:5173/api`
- PostgreSQL: `localhost:5432`

On first startup, the app applies migrations and seeds rich demo data automatically (configured in `.env` / `Seed` settings).

Stop services:

```powershell
docker compose down
```

Remove services and DB volume:

```powershell
docker compose down -v
```

### Option B: Local development (without Docker)

Prerequisites:
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) LTS + npm
- PostgreSQL running locally

Backend:

```powershell
cd backend
dotnet run --project TaskFlow.API
```

Frontend:

```powershell
cd frontend
npm install
npm run dev
```

Useful build checks:

```powershell
dotnet build backend/TaskFlow.slnx -c Release
cd frontend; npm run build
```

## Demo Credentials (Client Walkthrough)

Use these accounts after seeding:

- Platform admin:
  - Email / Username: `admin@taskflow.local`
  - Password: `Admin123!` (local settings) or your configured `SEED_ADMIN_PASSWORD` in Docker
- Demo tenant users:
  - Email / Username pattern: `demo.user001@taskflow.local` through `demo.user060@taskflow.local`
  - Password: `Demo123!` (or your configured `SEED_DEMO_USER_PASSWORD`)
  - Quick login examples:
    - `demo.user001@taskflow.local` / `Demo123!`
    - `demo.user002@taskflow.local` / `Demo123!`
    - `demo.user003@taskflow.local` / `Demo123!`
    - `demo.user010@taskflow.local` / `Demo123!`
    - `demo.user015@taskflow.local` / `Demo123!`
    - `demo.user020@taskflow.local` / `Demo123!`
    - `demo.user030@taskflow.local` / `Demo123!`
    - `demo.user040@taskflow.local` / `Demo123!`
    - `demo.user050@taskflow.local` / `Demo123!`
    - `demo.user060@taskflow.local` / `Demo123!`

Seed defaults generate at least:
- 12 organizations
- 60 users
- 72 projects
- 576 tasks

## Suggested Portfolio Screenshots

Include 5-7 screenshots in your portfolio or README:
- Landing page (`Home`) with clean branding and CTA
- Authentication flow (Login/Register)
- Workspace management (Create/Join workspace)
- Dashboard analytics (cards + charts)
- Projects management table (search/sort/edit)
- Tasks management screen (filters + inline edit)
- Swagger API docs (`/swagger`) to demonstrate backend quality

Tip: keep a consistent browser size (e.g., 1440x900), blur sensitive values, and use the same seed data so comparisons are clear.
