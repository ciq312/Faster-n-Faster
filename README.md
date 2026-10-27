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
  │                  bans)                cache)
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
│   └── *.cs         — area-wide constants (Races/RaceCountdown)
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

### Design decisions

How the system runs — where state lives, the race hot path, the concurrency model and what scaling out would take — is in [docs/architecture.md](docs/architecture.md). Each design decision, with its context, the alternatives and the trade-offs, has a record in [docs/decisions/](docs/decisions/README.md).

---

## API

### Error responses

Errors the API writes itself share one body, `ApiErrorResponse`:

```json
{ "reason": "<message for the user>", "code": "<machine code>", "errors": { "<key>": ["<value>"] } }
```

`reason` and `code` are always present; `errors` is `null` when there is nothing to put in it.

| Source | Status | `reason` | `code` | `errors` |
|---|---|---|---|---|
| Business rule (`StatusException`) | 400, 401, 403, 404, 409 | the exception's message | `error`, or a specific code (see below) | the exception's extra fields, or `null` |
| Request validation (FastEndpoints) | 400 | `One or more validation errors occurred.` | `validation_failed` | `{ "<field>": ["<message>"] }` |
| Missing or invalid access token | 401 | `Authentication required` | `not_authenticated` | `null` |
| Missing role | 403 | `Access denied` | `forbidden` | `null` |
| Rate limit | 429 | `Too many requests, try again shortly` | `rate_limited` | `null`; a `Retry-After` header gives the wait in seconds |
| Unhandled server error | 500 | `An unexpected error occurred` | `error` | `null` |

Show `errors` for a validation failure and `reason` otherwise; branch only on `code`, never on message text.

#### Error codes

| Code | Status | Endpoint | `errors` | Meaning |
|---|---|---|---|---|
| `email_not_verified` | 403 | `POST /api/auth/login` | `email` — the account's email | Credentials are correct but the email isn't verified. The client redirects to `/check-your-email?email=…`, where the user can resend the link. |
| `invalid_credentials` | 401 | `POST /api/auth/login` | — | Wrong login or password; the message doesn't say which. |
| `invalid_cursor` | 400 | `GET /api/lobbies` | — | `cursor` isn't one this endpoint issued. The client shows the reason; Refresh starts the feed again from the first page. |
| `invalid_invite_code` | 404 | `GET /api/lobbies/by-invite/{inviteCode}` | — | No lobby has this invite code. Joining a private lobby with a wrong code over the hub gives the same message without a code. |
| `not_on_leaderboard` | 404 | `GET /api/leaderboards/me` | — | The player has no statistics yet, or is banned or suspended, so has no leaderboard row. The client shows no pinned row. |
| `not_authenticated` | 401 | any endpoint that needs a login, `POST /api/auth/refresh` | — | No valid access token, or the refresh token is missing, expired or already rotated. |
| `forbidden` | 403 | endpoints with a role requirement | — | Logged in, but the role isn't allowed. |
| `rate_limited` | 429 | rate-limited endpoints | — | Too many requests; retry after `Retry-After`. |

```json
{ "reason": "Email verification required", "code": "email_not_verified", "errors": { "email": ["player@example.com"] } }
```

Every error gets its own code, whether or not a client branches on it yet; the client decides which ones to handle. Codes live in `ErrorCodes` (`FasterNFaster.Api.Domain/Exceptions/ErrorCodes.cs`) and are set by overriding `StatusException.Code`; extra fields come from `StatusException.Extensions` and go into `errors`.

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

# API in Docker (starts Postgres and Redis with it)
docker compose up api --build

# or: Postgres and Redis in Docker, API on the host with hot reload
docker compose up postgres redis
cd FasterNFaster.Api.Presentation && dotnet watch run

# frontend: Vite on :3000, proxies /api and /gameHub to the API on :8080
cd fasternfasterapp
npm install
npm run dev
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
3. **frontend-build** — `npm ci → lint → typecheck → format:check → test:run → build`
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
- Migrations run before the new API starts: the one-shot `migrate` service applies the EF Core migration bundle from the same image, and `api` starts only if it succeeds. A rollback does not undo them. The older version has to work with the newer schema.

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

Frontend tests use Vitest + Testing Library on jsdom: `npm test` (watch) or `npm run test:run` (once, what CI runs), from `fasternfasterapp/`.

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
