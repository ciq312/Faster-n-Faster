# Faster'n'Faster

A real-time multiplayer typing simulator. Players compete in public or private lobbies, watching each other's live progress as they type a passage. Includes leaderboards, personal statistics, post-race breakdowns, and full account management.

**Live at [faster-n-faster.com](https://faster-n-faster.com)**

---

## Features

- **Live multiplayer** — real-time cursor progress for every player in the lobby, updated keystroke by keystroke
- **Lobby system** — create or join public lobbies instantly; private lobbies generate a shareable invite code
- **Post-race stats** — WPM, accuracy, mistakes, and final ranking for every participant
- **Leaderboards** — global ranking across all races
- **Player profile** — personal stats history and account management
- **Auth** — email/password with email verification (via Resend) and Google OAuth
- **Server-authoritative** — every keystroke is validated server-side before progress is committed; the client only renders visual feedback locally

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | React + Vite |
| Backend | ASP.NET Core (.NET 10, FastEndpoints, EF Core) |
| Real-time | SignalR (WebSockets) |
| Persistent DB | PostgreSQL 15 |
| Ephemeral / pub-sub | Redis 7 |
| Email | Resend |
| Reverse proxy | Caddy (automatic TLS) |
| Containerization | Docker + Docker Compose |
| CI/CD | GitHub Actions → GHCR → VPS |

---

## Architecture

### Request flow

```
Browser
  │
  ▼
Caddy  (:80/:443, automatic Let's Encrypt TLS)
  ├── /api/* + /gameHub  ──→  api:8080  (ASP.NET Core)
  │                                │
  │                         ┌──────┴──────┐
  │                         ▼             ▼
  │                    PostgreSQL 15    Redis 7
  │               (users, lobbies,  (live race state,
  │                race results,     SignalR backplane)
  │                leaderboard)
  │
  └── everything else  ──→  frontend:3000  (nginx, serves built React)
```

### Backend layers

```
fasternfaster.api/
├── Core/
│   ├── Entities/       — pure domain models; enforce their own invariants
│   └── Exceptions/     — domain exceptions (LobbyFullException, etc.)
├── UseCases/
│   ├── Interfaces/     — service + store contracts
│   ├── Services/       — business logic (auth, lobby lifecycle, race ticks)
│   └── Factories/      — object construction
├── Web/
│   ├── Hubs/           — SignalR hub (thin: validate → call service → broadcast)
│   ├── Endpoints/      — FastEndpoints handlers, one per use-case
│   └── Options/        — strongly-typed config (JWT, SMTP, cookies, etc.)
└── infrastructure/
    └── Db/             — EF Core DbContext, migrations
```

Dependencies flow inward: `Web → UseCases → Core`. Infrastructure implements interfaces defined in `UseCases/Interfaces/` — nothing in the core references EF Core or PostgreSQL.

### Key design decisions

- **Race tick broadcaster** — instead of broadcasting full lobby state on every keystroke (which would be O(N²) messages per second), a background service runs on a 200 ms tick. Each tick it flushes a queue of pending progress updates and broadcasts a single slim event per player to the rest of the lobby. This keeps bandwidth and CPU flat regardless of typing speed.
- **Keystroke throttling** — the client throttles how often it sends keystrokes to the server, preventing the hub from being flooded under fast typists or network bursts
- **Redis as backplane** — enables horizontal scaling of the SignalR hub without sticky sessions
- **Host promotion** — if the host disconnects, the next player in the lobby list is automatically promoted
- **Disconnected player mid-race** — cursor freezes, race continues, player excluded from results
- **JWT auth** — RS256 asymmetric signing; short-lived access tokens + sliding refresh tokens in HttpOnly cookies

---

## Project Structure

```
Faster-n-Faster/
├── fasternfaster.api/      — ASP.NET Core backend
├── fasternfaster.tests/    — xUnit test project
├── fasternfasterapp/       — React + Vite frontend
│   └── src/
│       ├── features/       — auth, game, lobbies, leaderboard, connection
│       ├── pages/          — routed page components
│       └── shared/         — reusable components, utils
├── profiling/              — performance profiling tools (see below)
├── loadtest/               — SignalR load test bot (.NET)
├── docker-compose.yml      — local dev stack
├── docker-compose.prod.yml — prod overlay (pulls images from GHCR)
└── Caddyfile               — reverse proxy config
```

---

## Dev Setup

**Requirements:** Node.js 22, .NET 10 SDK, Docker

```bash
cp .env.example .env   # fill in values
docker compose up --build
```

---

## CI/CD

On push to `main`:

1. **backend-build** — `dotnet restore → build → test`
2. **frontend-build** — `npm ci → npm run build`
3. **publish** — Docker images pushed to GHCR tagged `:latest` and `:<sha>`
4. **deploy** — SSH to VPS, `git pull`, `docker compose pull`, `docker compose up -d`, prune old images

Rollback: re-deploy with an explicit `:<sha>` image tag.

---

## Production

Hosted on a Time4VPS VPS (4 GB RAM). All services run as Docker containers:

| Container | Image | Role |
|---|---|---|
| `caddy` | `caddy:latest` | TLS termination, automatic Let's Encrypt, routing |
| `frontend` | `ghcr.io/ciq312/fasternfaster-frontend:latest` | nginx serving built React on :3000 |
| `backend` | `ghcr.io/ciq312/fasternfaster-api:latest` | ASP.NET Core API on :8080 |
| `postgresDB` | `postgres:15` | Persistent storage |
| `redis` | `redis:7-alpine` | Ephemeral state + SignalR backplane (256 MB LRU) |

---

## What's Being Refactored

- **MediatR / CQRS** — the service layer is being migrated to a proper CQRS pattern using MediatR. Commands and queries are being extracted from services into dedicated handlers. MediatR is not yet wired into the DI container; this is in progress.
- **Dependency cleanup** — some packages pulled in early are being evaluated and removed where they add weight without value.
- **Automated profiling pipeline** — load testing, counter collection, and graph generation are currently a manual process (SSH to server, run `dotnet-counters`, copy CSV, run `profiler.py` locally). The goal is to automate this end-to-end: trigger a load test from CI, collect counters remotely, and produce graphs automatically.

---

## Performance Tooling

### Load testing (`loadtest/`)

A .NET SignalR bot that simulates concurrent players joining lobbies and racing. Results at various loads (10–1000 concurrent users) are checked in as CSV alongside their phase timings.

```bash
cd loadtest
dotnet run -- --server https://faster-n-faster.com --users 500 --wpm 120
```

Flags: `--server <URL>` (default `http://localhost:8080`), `--users <N>` (default 5), `--wpm <N>` (default 120), `--insecure` (skip TLS verification).

### Profiling (`profiling/`)

`profiler.py` reads `dotnet-counters` CSV exports and renders graphs for CPU, memory, thread pool, GC allocation, and JIT metrics. Supports single-run and overlay modes for comparing runs side by side.

```bash
# Single run
python profiler.py counters_500.csv phases500users.json

# Overlay — compare two scenarios
python profiler.py "before=counters_100.csv,phases100users.json" "after=counters_200.csv,phases200users.json"
```

Collect counters from the running backend container:

```bash
docker exec -d backend sh -c '/opt/tools/dotnet-counters collect -p 1 \
  --format csv -o /tmp/counters.csv \
  --counters System.Runtime,Microsoft.AspNetCore.Hosting & echo $! > /tmp/counters.pid'

# Stop collection
docker exec backend sh -c 'kill -INT $(cat /tmp/counters.pid)'

# Copy CSV to host
docker cp backend:/tmp/counters.csv ./profiling/my_run.csv
```
