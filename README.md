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
| Tokens / caching | Redis 7 |
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
  │                    PostgreSQL 15      Redis 7
  │                 (users, statistics,  (refresh/confirm
  │                  race results,        tokens, leaderboard
  │                  bans)                + ban caches)
  │
  │          live lobby & race state is held in-memory
  │          inside the API (single instance by design)
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
- **In-memory live state** — lobbies and race progress live in lock-protected in-memory stores inside the API, keeping the hot path free of network round-trips; Redis holds refresh/confirm tokens and read caches. The trade-off is a single API instance — scaling out would mean adding a Redis SignalR backplane and externalizing lobby state
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
├── loadtest/               — SignalR load test bot (.NET)
├── docker-compose.yml      — local dev stack
├── docker-compose.prod.yml — prod overlay (pulls images from GHCR)
└── Caddyfile               — reverse proxy config
```

Load-test orchestration and profiling live in a separate repo: [Faster-n-Faster-profiling](https://github.com/ciq312/Faster-n-Faster-profiling).

---

## Dev Setup

**Requirements:** Node.js 22, .NET 10 SDK, Docker

```bash
cp .env.example .env   # fill in values

cd fasternfasterapp 
npm install
npm run dev

cd fasternfaster.api
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
| `redis` | `redis:7-alpine` | Tokens + read caches (256 MB LRU) |

---

## What's Being Refactored

- **MediatR / CQRS** — commands and queries have been extracted from services into dedicated MediatR handlers. The race progress hot path deliberately bypasses MediatR to avoid per-keystroke allocations; a few remaining service methods are still being migrated.
- **Dependency cleanup** — some packages pulled in early are being evaluated and removed where they add weight without value.
- **CI-triggered profiling** — load testing, counter collection, and graph generation are automated end-to-end by [`automation.py`](https://github.com/ciq312/Faster-n-Faster-profiling): one command runs the bot swarm on the server, collects `dotnet-counters` metrics from the backend container, and renders phase-annotated graphs locally. The next step is triggering these runs from CI.

---

## Performance Tooling

### Load testing (`loadtest/`)

A .NET SignalR bot that simulates concurrent players joining lobbies and racing. Results at various loads (10–2000 concurrent users) are checked into the [tools repo](https://github.com/ciq312/Faster-n-Faster-profiling) as CSV alongside their phase timings.

```bash
cd loadtest
dotnet run -- --server https://faster-n-faster.com --users 500 --wpm 120
```

Flags: `--server <URL>` (default `http://localhost:8080`), `--users <N>` (default 5), `--wpm <N>` (default 120), `--insecure` (skip TLS verification).

### Profiling & orchestration ([Faster-n-Faster-profiling](https://github.com/ciq312/Faster-n-Faster-profiling))

The full load-test → profile → graph pipeline is automated in a separate repo. One command drives the server over SSH, runs the bot swarm, collects `dotnet-counters` metrics from the backend container, and renders phase-annotated graphs (CPU, memory, thread pool, GC, JIT) locally:

```bash
python automation.py --users 500 --label 500UsersProd --wpm 120
```

Raw counter data and generated graphs for every experiment (semaphore fixes, allocation reductions, tick-rate changes, 10–2000 users) are checked in there alongside `profiler.py`, which also supports overlay mode for comparing runs side by side.
