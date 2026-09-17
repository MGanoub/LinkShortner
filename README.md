# LinkShortener

A full-stack URL shortener built with ASP.NET Core, PostgreSQL, and React with authentication, rate limiting, and a fully containerized, CI-tested, deployed pipeline.

**Live demo:** [linkshortner-3hzn.onrender.com](https://linkshortner-3hzn.onrender.com/)

> Free-tier hosting note: the backend spins down after 15 minutes of inactivity, so the first request after a while may take 30–60 seconds to wake up.

<img width="991" height="906" alt="image" src="https://github.com/user-attachments/assets/2cfdf2ec-1420-4939-b203-5e68d98bfb3d" />

## Features

- Shorten any valid URL into a short, shareable code
- Optional link expiry (`expiresInDays`)
- Click tracking per link
- User accounts: register/login with JWT auth; each user's links are tied to their account
- Rate limiting on link creation to prevent abuse
- Fully typed React + TypeScript frontend with a custom "departure board" UI
- Dockerized backend + Postgres, with a docker-compose setup for local development
- CI pipeline (GitHub Actions) running the full test suite  including real Postgres via Testcontainers on every push

## Tech stack

**Backend:** C# / ASP.NET Core minimal API, Entity Framework Core, PostgreSQL (Npgsql), JWT authentication, `System.Threading.RateLimiting`

**Frontend:** React, TypeScript, Vite

**Testing:** xUnit, Testcontainers (real Postgres per test run), FluentAssertions

**Infra:** Docker, Docker Compose, GitHub Actions, Render (hosting)

## Project structure
```
LinkShortner/
├── LinkShortner/ # ASP.NET Core API
│ ├── Endpoints/ # Minimal API endpoint definitions
│ ├── Models/ # EF Core entities + request/response records
│ ├── Data/ # DbContext
│ ├── Services/ # TokenService, ShortCodeGenerator
│ ├── Migrations/ # EF Core migrations
│ └── Dockerfile
├── LinkShortener.Tests/ # xUnit integration + unit tests
├── linkshortener-frontend/ # React + TypeScript frontend
├── .github/workflows/ # CI pipeline
└── docker-compose.yml # Local dev: Postgres + API together
```

## Running locally

### Prerequisites
- .NET 10 SDK
- Node.js + npm
- Docker Desktop

### 1. Start Postgres + API via Docker Compose

```bash
docker compose up -d --build
```

This builds the API image and starts it alongside a Postgres container. Copy `.env.example` to `.env` first and fill in real values (JWT signing key, DB password) see that file for the required variables.

### 2. Run database migrations

```bash
cd LinkShortner
dotnet ef database update
```

### 3. Run the frontend

```bash
cd linkshortener-frontend
npm install
cp .env.example .env   # set VITE_API_BASE to your local backend URL
npm run dev
```

Visit `http://localhost:5173`.

## Running tests

```bash
cd LinkShortener.Tests
dotnet test
```

Tests spin up a real, throwaway Postgres container via Testcontainers: no manual database setup required, and no shared state between runs. Requires Docker to be running.

## API overview

| Method | Endpoint | Auth required | Description |
|---|---|---|---|
| POST | `/auth/register` | No | Create an account, returns a JWT |
| POST | `/auth/login` | No | Log in, returns a JWT |
| POST | `/shorten` | Yes | Create a short link |
| GET | `/{code}` | No | Redirect to the original URL, increments click count |
| GET | `/api/urls/{code}` | No | Look up a link's metadata |
| GET | `/api/urls/mine` | Yes | List the authenticated user's own links |

## Deployment

Deployed on Render: PostgreSQL, the API (as a Docker web service), and the frontend (as a static site) each run as separate Render services. Environment variables (connection strings, JWT signing key, CORS origins, rate limit settings) are configured per-service in Render's dashboard, not committed to source.

## What this project was built to practice

- Minimal API design in ASP.NET Core
- EF Core migrations and relational modeling
- JWT-based authentication from scratch
- Integration testing with real infrastructure (Testcontainers) rather than mocks
- CORS, rate limiting, and other production-readiness concerns
- CI/CD and containerized deployment end-to-end
