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
├── Core/                — domain layer, zero external dependencies
│   ├── Entities/        — aggregates that enforce their own invariants and raise domain events
│   ├── Exceptions/      — domain exceptions (LobbyFullException, CheaterDetectedException, ...)
│   └── Interfaces/      — domain contracts (IDomainEvent, IEventDispatcher, IAntiCheatPolicy)
├── UseCases/            — application layer, feature-sliced CQRS
│   ├── Users/           — RegisterUser, LoginUser, VerifyEmail, ... (Command + Handler pairs)
│   ├── Lobbies/         — CreateLobby, JoinLobby, StartRace, KickPlayer, Disconnect, ...
│   ├── Leaderboards/    — leaderboard query
│   ├── Realtime/        — Broadcast* handlers that react to domain events
│   ├── Interfaces/      — ports implemented by Infrastructure and Web
│   └── Services/        — hot-path orchestration (lobby/race lifecycle)
├── Infrastructure/      — adapters: EF Core/Postgres repositories, Redis cache + token stores,
│                          SMTP, in-memory lobby store, race tick loop + conflator
└── Web/
    ├── Hubs/            — SignalR hub (thin: resolve context → call facade) + hub filters
    ├── <feature>/       — FastEndpoints, one folder per use case (Users/, Lobbies/, ...)
    └── Options/         — strongly-typed config (JWT, SMTP, cookies, rate limits, anti-cheat)
```

Dependencies flow inward: `Web → UseCases → Core`. Infrastructure implements interfaces defined in `UseCases/Interfaces/` — nothing in the core references EF Core, Redis, or SignalR.

### Race hot path

Every keystroke batch is validated server-side, but broadcasting is decoupled from typing speed — progress lands in in-memory state, and a 200 ms tick loop conflates it into one slim broadcast per lobby.

The conflator keeps a single "latest frame" slot per lobby: if a broadcast is still in flight when the next tick lands, the old frame is replaced rather than queued, so a slow connection can never build a backlog of stale race state.

### Key design decisions

- **Race tick broadcaster** — instead of broadcasting full lobby state on every keystroke (which would be O(N²) messages per second), a background service runs on a 200 ms tick and broadcasts one conflated frame per lobby (see [Race hot path](#race-hot-path)). This keeps bandwidth and CPU flat regardless of typing speed.
- **CQRS with a deliberate hot-path bypass** — every use case is a MediatR `Command` + `Handler` pair (`UseCases/Users/RegisterUser/`, `UseCases/Lobbies/JoinLobby/`, ...), keeping handlers small and independently testable. The one exception is race progress: keystrokes go straight from the hub to the race service to avoid per-keystroke mediator allocations.
- **Domain events over direct coupling** — aggregates raise events (`PlayerJoinedEvent`, `RaceFinishedEvent`, ...) that are dispatched as MediatR notifications; broadcast handlers in `UseCases/Realtime/` subscribe and push the SignalR messages. The domain never references SignalR, and a new side effect is a new handler, not an edit to existing code.
- **Decorator-pattern caching** — `CachedBanRepository`, `CachedLeaderboardRepository`, and `CachedStatisticsRepository` wrap the Postgres repositories with Redis cache-aside logic. Callers depend on the same interface either way; caching is composed in DI and can be removed without touching business code.
- **Cross-cutting concerns as hub filters** — ban checks, cheat detection, and exception mapping live in dedicated SignalR hub filters (`HubBanFilter`, `HubCheatFilter`, `HubExceptionFilter`) instead of being repeated inside hub methods.
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

## Testing

xUnit, no mocking framework — every dependency has a hand-rolled fake in `fasternfaster.tests/Fakes/`, which keeps test setup explicit and readable. A few worth noting:

- **`GatedRaceBroadcaster`** — lets a test hold a broadcast "in flight" and assert the conflator's latest-frame behavior deterministically, without sleeps.
- **`InMemoryCache` + fake repositories** — the cached repository decorators are tested against the real cache-aside logic over controlled backing stores.

Coverage spans use-case handlers, the caching decorators, the race tick/conflation/result pipeline, entity invariants, and request validators.

---

## Roadmap

- **CI-triggered profiling** — the load-test → profile → graph pipeline is already one command ([`automation.py`](https://github.com/ciq312/Faster-n-Faster-profiling)); next step is triggering runs from CI so every deploy gets a performance baseline.
- **Dependency cleanup** — packages pulled in early are being evaluated and removed where they add weight without value.

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

### Improvements this tooling paid for

- **Allocation pressure on the race tick.** Under load, the GC allocation-rate graph spiked during the racing phase. The culprit was `ParticipantSnapshot`: every 200 ms tick builds one snapshot per player per racing lobby, and as a `record` (class) each one was a heap allocation that died within the same tick — thousands of short-lived objects per second at high player counts. Changing it to a `record struct` stores snapshots inline in the frame list, cutting the allocation rate significantly. With the GC out of the hot path, broadcast latency under load dropped by over 200 ms RTT.
