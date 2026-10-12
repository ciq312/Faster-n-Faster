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

Every layer below Domain is split into the same feature areas: `Auth`, `Users`, `Lobbies`, `Races`, `AntiCheat`, `Realtime`, `Leaderboards`, plus a `Shared` folder for cross-cutting code. A layer only has an area folder when it has code for that area.

```
FasterNFaster.Api.Domain/          — domain layer, zero external dependencies
├── Entities/        — aggregates that enforce their own invariants and raise domain events
├── Exceptions/      — domain exceptions (LobbyFullException, ColorIsAlreadyTakenException, ...)
└── Interfaces/      — domain contracts (IDomainEvent, IEventDispatcher, IAntiCheatPolicy)
FasterNFaster.Api.UseCases/        — application layer, feature-sliced CQRS
├── <Area>/
│   ├── <UseCase>/   — Command/Query + Handler (+ Result), e.g. Auth/LoginUser, Lobbies/JoinLobby
│   ├── Ports/       — interfaces implemented by Infrastructure or Presentation (e.g. ILobbyStore, IRaceStore),
│   │                  plus the types in their signatures
│   ├── Services/    — application services shared by several use cases (LobbyState tracking/query,
│   │                  confirm tokens, user profiles)
│   ├── Exceptions/  — area-specific exceptions
│   └── *.cs         — area-wide constants (Realtime/GameEvents, Races/RaceCountdown)
└── Shared/          — domain-event dispatch (MediatR), IUnitOfWork, AggregateGate
FasterNFaster.Api.Infrastructure/  — adapters, grouped by the same areas
├── <Area>/          — Postgres/Redis repositories, SMTP, in-memory lobby/race stores, race tick loop + conflator
└── Shared/          — Persistence (AppDbContext, Migrations), Caching, AppOptions
FasterNFaster.Api.Presentation/
├── Web/<Area>/      — FastEndpoints, one folder per use case; Web/Realtime holds the SignalR hub,
│                      hub filters and broadcaster
├── Web/Shared/      — exception handler, health endpoint, rate-limit options
└── Extensions/      — DI composition root
```

Dependencies flow inward: `Presentation → Infrastructure → UseCases → Domain`. Infrastructure implements the `Ports/` interfaces of each UseCases area — nothing in Domain or UseCases references EF Core, Redis, or SignalR.

### Race hot path

Every keystroke batch is validated server-side, but broadcasting is decoupled from typing speed — progress lands in in-memory state, and a 200 ms tick loop conflates it into one slim broadcast per lobby.

The conflator keeps a single "latest frame" slot per lobby: if a broadcast is still in flight when the next tick lands, the old frame is replaced rather than queued, so a slow connection can never build a backlog of stale race state.

### Key design decisions

- **Race tick broadcaster** — instead of broadcasting full lobby state on every keystroke (which would be O(N²) messages per second), a background service runs on a 200 ms tick and broadcasts one conflated frame per lobby (see [Race hot path](#race-hot-path)). This keeps bandwidth and CPU flat regardless of typing speed.
- **CQRS with a deliberate hot-path bypass** — every use case is a MediatR `Command` + `Handler` pair (`UseCases/Auth/RegisterUser/`, `UseCases/Lobbies/JoinLobby/`, ...), keeping handlers small and independently testable. The one exception is race progress: keystrokes go straight from the hub to `IRaceStore.TryMutate` to avoid per-keystroke mediator allocations.
- **Domain events over direct coupling** — aggregates raise events (`PlayerJoinedEvent`, `RaceFinishedEvent`, ...) that are dispatched as MediatR notifications; broadcast handlers in `UseCases/Realtime/` subscribe and push the SignalR messages. The domain never references SignalR, and a new side effect is a new handler, not an edit to existing code.
- **Decorator-pattern caching** — `CachedLeaderboardRepository` wraps the Postgres leaderboard queries with Redis cache-aside logic. Callers depend on the same interface either way; caching is composed in DI and can be removed without touching business code.
- **Cross-cutting concerns as hub filters** — ban checks, suspension checks, and exception mapping live in dedicated SignalR hub filters (`HubBanFilter`, `HubSuspensionFilter`, `HubExceptionFilter`) instead of being repeated inside hub methods.
- **Keystroke throttling** — the client throttles how often it sends keystrokes to the server, preventing the hub from being flooded under fast typists or network bursts
- **In-memory live state** — lobbies and race progress live in in-memory stores inside the API, keeping the hot path free of network round-trips; Redis holds refresh/confirm tokens and read caches. The trade-off is a single API instance — see [Scaling out (later)](#scaling-out-later).
- **Stores are ports; state stays behind a gate** — `ILobbyStore` and `IRaceStore` live in `UseCases/<Area>/Ports` and are implemented by `InMemoryLobbyStore` / `InMemoryRaceStore`. Each owns its dictionary and an `AggregateGate<T>` (one async semaphore per aggregate): mutations run inside the gate and their domain events are dispatched after it is released; reads take a projection that runs inside the gate, so no live aggregate ever leaves a store. The lobby store also owns the player→lobby and invite-code indexes and keeps them in sync with the lobbies; "one lobby per player" is enforced by an atomic claim on that index from `Lobby.Join`.
- **LobbyState flush** — handlers mark the lobbies they changed on `ILobbyStateTracker`; `LobbyStateFlushBehavior` broadcasts one `LobbyState` per changed lobby when the command finishes. Callers mark, not the store, so another store implementation doesn't have to repeat it.
- **Host promotion** — if the host disconnects, the next player in the lobby list is automatically promoted
- **Disconnected player mid-race** — cursor freezes, race continues, player excluded from results
- **JWT auth** — RS256 asymmetric signing; short-lived access tokens + sliding refresh tokens in HttpOnly cookies

### Deferred: more race modes

Today there is one sealed `Race` (fixed passage, word count). The split below waits for a second mode (TimerRace) or for changing the mode of an existing lobby — until then it is more moving parts than the feature justifies:

- **`WordRaceBuilder` / `TimerRaceBuilder`** — one concrete class per mode that owns its construction (parameters + passage loading); no shared interface until something needs polymorphism.
- **`LobbyCreator`** — generic lobby creation (name, privacy, host, invite code, store) that takes an already-built race.
- **Per-mode create handlers** (`CreateWordLobbyHandler`, `CreateTimerLobbyHandler`) — call the matching builder and hand the race to `LobbyCreator`.
- **Per-mode change handlers** (`ChangeToWordRaceHandler`, `ChangeToTimerRaceHandler`) — reuse the builders to swap the race of an existing lobby.
- **Wire settings** stay a DTO in `UseCases` (a sealed, discriminated hierarchy once there are two modes), never a type on the domain entity.

### Scaling out (later)

The API runs as a single instance on purpose: lobby and race state live in its memory. Running several instances would need:

- **A Redis SignalR backplane** so a broadcast reaches players connected to other instances.
- **A `RedisLobbyStore` behind `ILobbyStore`** — lobbies serialized to Redis, a distributed lock or optimistic versioning in place of the in-process gate, and the player→lobby and invite-code indexes as Redis keys (the player claim becomes `SET NX`). Handlers only talk to the port and mark LobbyState changes themselves, so they don't change.
- **Race state pinned to one instance** — the keystroke hot path stays in memory on the instance that owns the race; the lobby records the owner and that instance's tick loop broadcasts, instead of a Redis round-trip per keystroke.
- **Per-instance registries made shared or sticky** — strikes (`StrikeRegistry`), pending reconnect removals (`PendingRemovalRegistry`), sessions (`InMemorySessionService`) and the race tick registry are in-process today.

---

## API

### Error responses

The shape of an error body depends on where the error comes from:

| Source | Status | Body |
|---|---|---|
| Business rule (`StatusException`) | 400, 401, 403, 404, 409 | `{ "error": "<message>" }`, plus `code` and any extra fields the exception sets (see below) |
| Request validation (FastEndpoints) | 400 | `{ "statusCode": 400, "message": "One or more errors occurred!", "errors": { "<field>": ["<message>"] } }`, served as `application/problem+json` |
| Rate limit | 429 | `{ "message": "Too many requests, try again shortly." }` and a `Retry-After` header in seconds |
| Missing or invalid access/refresh token, missing role | 401 / 403 | empty body |
| Unhandled server error | 500 | ProblemDetails, `application/problem+json` (`type`, `title`, `status`, `traceId`) |

Show `error` / `errors` / `message` to the user; branch only on `code`, never on message text.

#### Error codes

| Code | Status | Endpoint | Extra fields | Meaning |
|---|---|---|---|---|
| `EMAIL_NOT_VERIFIED` | 403 | `POST /api/auth/login` | `email` — the account's email | Credentials are correct but the email isn't verified. The client redirects to `/check-your-email?email=…`, where the user can resend the link. |

```json
{ "error": "Email verification required", "code": "EMAIL_NOT_VERIFIED", "email": "player@example.com" }
```

Add a code only when a client branches on it. Codes live in `ErrorCodes` (`FasterNFaster.Api.Domain/Exceptions/ErrorCodes.cs`) and are set by overriding `StatusException.Code`; extra fields come from `StatusException.Extensions` and are written flat next to `error`, never overwriting `error` or `code`. Exceptions that set neither keep the plain `{ "error" }` body.

SignalR hub method errors carry only a message (`HubException`) and have no codes.

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

`main` is production — every merge deploys. `dev` collects finished work.

```
feature/* ──PR, squash──▶ dev ──PR, merge commit──▶ main ──▶ deploy
```

- Direct pushes, force pushes and deletion are blocked on `main` and `dev`.
- `main` only accepts PRs from `dev`, so hotfixes take the same path.
- PR titles must follow [Conventional Commits](https://www.conventionalcommits.org) (`feat: …`, `fix(lobby): …`) — the title becomes the commit message.

On every PR into `dev` or `main` (all must pass to merge):

1. **backend-build** — `dotnet restore → build → unit tests`
2. **integration-tests** — integration test project
3. **frontend-build** — `npm ci → npm run lint -> npm run build`
4. **pr-title** — Conventional Commits check; **release-source** — PRs into `main` come from `dev`

On push to `main` (a merged release PR), after the builds pass again:

1. **publish** — Docker images pushed to GHCR tagged `:latest` and `:<sha>`
2. **deploy** ([`deploy.yml`](.github/workflows/deploy.yml)) — SSH to VPS, `git reset --hard <sha>`, `docker compose pull` + `up -d` with `IMAGE_TAG=<sha>`, remove older images

The VPS checkout and both images are pinned to the same commit, so the compose files and Caddyfile always match the running code. Every deploy and its SHA is listed under the repo's **Environments → production**.

### Rollback

Re-run the deploy with an earlier SHA, either from the Actions tab (**Deploy → Run workflow**) or with:

```bash
gh workflow run deploy.yml -f sha=<full commit sha>
```

- Only commits pushed to `main` have images. Any other SHA fails at `docker compose pull`, and nothing is restarted.
- The API applies EF Core migrations on startup, and a rollback does not undo them. The older version has to work with the newer schema.

### On the server

The deploy writes `IMAGE_TAG=<sha>` into `/opt/Faster-n-Faster/.env`, so manual `docker compose -f docker-compose.yml -f docker-compose.prod.yml …` commands stay on the deployed version. To see what's running:

```bash
git -C /opt/Faster-n-Faster rev-parse HEAD
docker compose -f docker-compose.yml -f docker-compose.prod.yml images
```

---

## Production

Hosted on a Contabo VPS (8 GB RAM). All services run as Docker containers:

| Container | Image | Role |
|---|---|---|
| `caddy` | `caddy:latest` | TLS termination, automatic Let's Encrypt, routing |
| `frontend` | `ghcr.io/ciq312/fasternfaster-frontend:<sha>` | nginx serving built React on :3000 |
| `backend` | `ghcr.io/ciq312/fasternfaster-api:<sha>` | ASP.NET Core API on :8080 |
| `postgresDB` | `postgres:15` | Persistent storage |
| `redis` | `redis:7-alpine` | Tokens + read caches (256 MB LRU) |

---

## Testing

xUnit, no mocking framework — every dependency has a hand-rolled fake in `FasterNFaster.UnitTests/Fakes/`, which keeps test setup explicit and readable. A few worth noting:

- **`GatedBroadcaster`** — lets a test hold a broadcast "in flight" and assert the conflator's latest-frame behavior deterministically, without sleeps.
- **`InMemoryCache` + fake repositories** — the cached repository decorators are tested against the real cache-aside logic over controlled backing stores.

Coverage spans use-case handlers, the caching decorators, the race tick/conflation/result pipeline, entity invariants, and request validators.

---

## Roadmap

- **CI-triggered profiling** — the load-test → profile → graph pipeline is already one command ([`automation.py`](https://github.com/ciq312/Faster-n-Faster-profiling)); next step is triggering runs from CI so every deploy gets a performance baseline.
- **Dependency cleanup** — packages pulled in early are being evaluated and removed where they add weight without value.
- **Frontend switch to typescript** — currently the frontend is javascript and I'm considering switching to Typescript 

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
