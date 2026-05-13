# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Faster-n-Faster** is a real-time multiplayer typing race web application. Players enter a display name (no auth in v1) and compete in public or private lobbies. Live progress, speed-based taunts/hype comments, and post-race stats (WPM, accuracy, mistakes) are core features.

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | React |
| Backend | ASP.NET Core (C#) |
| Real-time | SignalR (WebSockets) |
| Persistent DB | PostgreSQL |
| Ephemeral / pub-sub

## Architecture

- **Server-authoritative typing validation**: The client only renders visual feedback locally. Every keystroke index is sent to the server, which validates it against the source passage before updating Redis and broadcasting progress. Never trust client-reported progress.
- **SignalR hub**: Handles connect/disconnect, countdown broadcasts, keystroke validation responses, live comment broadcasts, and race end events. Redis is the SignalR backplane for horizontal scaling.
- **Live comments engine**: Server-side — evaluates per-player WPM against configurable thresholds (stored in PostgreSQL, not hardcoded), broadcasts comment + player name to the whole lobby when a threshold is crossed. Per-player cooldown prevents spam.

## PostgreSQL Schema (planned)

Tables: `lobbies`, `lobby_players`, `race_results`, `comment_thresholds`

- `userId` in player records is nullable — designed so auth can be added later without a full schema rewrite.
- Invite codes for private lobbies: 6–8 character alphanumeric, must be unique (regenerate on collision).

## Key Behavioral Rules

- **Paste is blocked on the client** as a UX convenience, but server validation is the real enforcement layer.
- **Host promotion**: if the host disconnects, promote the next player in the lobby list.
- **Disconnected player mid-race**: cursor freezes, race continues, player excluded from results.
- **Tiebreaker**: server timestamp when two players finish simultaneously.
- **Comment thresholds** must be configurable (DB or config file), not hardcoded, so they can be tuned without redeploy.

## Game Modes (v1)

- **Word count**: type a fixed number of words as fast as possible.
- **Timer**: type as many words as possible within a time limit (e.g., 60s).

## Resolved Design Decisions

- **Max players per lobby**: 30
- **Typing passages**: fixed text initially; random selection from a pool is a future enhancement
- **WPM calculation**: gross vs. net is undecided — design the calculation so it's switchable per player preference
- **Minimum players**: solo play is allowed (no minimum to start a race)
- **Private lobby lifespan**: invite code and lobby persist after race ends so the host can start a rematch
- **Display name validation**: length-limited (exact max TBD); no profanity filter in v1
- **Live comments**: each player sees their own WPM-based comment — comments are personal, not only visible to others
- **Comments UI**: displayed at the top of the page (banner/overlay style)

## Code Architecture — Pragmatic Clean Architecture

Follow clean architecture principles without overkill. The goal is separation of concerns so that a change in one area (database, API shape, business rules) doesn't ripple through unrelated code.

### Backend Structure (`fasternfaster.api/`)

```
Models/        — Entity classes. Pure data + business rules. No dependencies on EF, HTTP, or external libraries.
DTOs/          — Request/response objects for the API. Never expose entities directly to clients.
Interfaces/    — Contracts (IRepository<T>, ILobbyService, etc.). Defined here so inner layers don't depend on implementations.
Services/      — Business logic and orchestration. Depend on interfaces, never on DbContext or controllers directly.
Data/          — DbContext, repository implementations, migrations, entity configurations. The only place that knows about PostgreSQL.
Hubs/          — SignalR hubs. Thin — validate input, call services, broadcast results.
FastEndPoints/ endpoints for every new request```

### Rules to Follow

1. **Dependencies flow inward.** Controllers/Hubs → Services → Interfaces ← Data. Never the reverse. A service must not reference a controller. Data implements interfaces but is never referenced by services directly.

2. **Endpoints and Hubs are thin.** They handle HTTP/SignalR concerns (request parsing, validation, response shaping) and delegate to services. No business logic in controllers or hubs. If a method has an `if` that isn't about validation or response codes, it belongs in a service.

3. **Services contain business logic.** "If the lobby is private, generate an invite code" lives in a service (or the entity itself), not in a controller. Services depend on interfaces (`IRepository<T>`), not on `AppDbContext` directly.

4. **Entities own their invariants.** Use `private set` on properties. If an entity has rules about its own state (e.g., "status can only transition from waiting → racing → finished"), enforce them inside the entity with methods. Don't let outside code set properties freely.

5. **Use DTOs at API boundaries.** Never return an entity directly from an end point. Map to a DTO that contains only what the client needs. This decouples your database schema from your API contract.

6. **Program against interfaces for external dependencies.** Anything that talks to a database, cache, or external service should have an interface. This makes the code testable and swappable. Don't create interfaces for things that won't change (e.g., string utilities, pure functions).

7. **Don't over-abstract.** No mediator, no specification pattern, no domain events — unless a concrete need arises. A direct service call is fine. Add patterns only when the simpler approach causes real pain (duplication, tight coupling, testing difficulty).

8. **Repository pattern is optional.** If a service needs simple CRUD, injecting `AppDbContext` through an interface is fine. Add a repository when query logic is reused across multiple services or when you need to abstract the data layer for testing.

### When to Add More Structure

- **Add a repository** when the same query appears in 2+ services
- **Add a domain event** when an action triggers side effects in unrelated areas (e.g., "race finished" triggers result saving AND comment cleanup)
- **Split into separate projects** if the codebase grows beyond ~30 files per layer
- **Add mediator/CQRS** only if you need multiple entry points (REST + SignalR + background jobs) triggering the same logic

### When NOT to Add Structure

- Don't wrap a simple `dbContext.Lobbies.FindAsync(id)` in a repository if it's used once
- Don't create a service for logic that's just one line of code
- Don't create interfaces for classes with no reason to be swapped
- Don't add DTOs for internal communication between services — DTOs are for API boundaries

## Dev Environment Notes

- Environment config via `.env`: DB connection strings, Redis URL, CORS origins.
- Docs are in `Faster'n'FasterDocs/General/` (Obsidian vault) — `spec.md` is the authoritative spec.

## Production Deployment

The app runs on a Hetzner-style VPS in Lithuania (≈3.8 GB RAM, no swap configured) at `/opt/Faster-n-Faster`. Everything is Dockerized.

### Topology

```
Browser
  │
  ▼
Caddy (container, ports 80/443)         — TLS termination, gzip/zstd, automatic Let's Encrypt
  │
  ├─ /api/*, /gameHub → currently routed by frontend nginx (suboptimal — see "Known issues")
  └─ everything else  → frontend:3000
                          │
                          ▼
                       Frontend container (nginx:alpine on :3000)
                          ├─ serves built React static files from /usr/share/nginx/html
                          └─ proxies /api/ and /gameHub to api:8080  ← extra hop
                                                    │
                                                    ▼
                                              API container (dotnet FasterNFaster.Api.dll on :8080)
                                                    │
                                                    ├──→ postgresDB (postgres:15)
                                                    └──→ redis (redis:7-alpine, 256mb maxmemory, allkeys-lru)
```

- Composition: `docker-compose.yml` + `docker-compose.prod.yml` (prod overlay swaps `build:` for prebuilt `ghcr.io/ciq312/...:latest` images and removes published ports for non-edge services).
- Services: `postgresDB`, `redis`, `backend` (the API, container_name), `front` (the frontend, container_name), `caddy`.
- Caddy talks to other containers via Docker's internal DNS (service names: `api`, `frontend`).
- The Caddyfile currently only routes everything to `frontend:3000`. The frontend container's `nginx.conf` proxies `/api/` and `/gameHub` upstream to `api:8080`. This means **two reverse proxies sit between Caddy and the API** — should be collapsed into one (route in Caddy directly).

### CICD (`.github/workflows/CICD.yml`)

- `backend-build`: `dotnet restore/build/test` on Ubuntu, .NET 8.
- `frontend-build`: `npm ci --legacy-peer-deps && npm run build` on Node 22.
- `publish-api` / `publish-frontend`: build and push images to GHCR (`ghcr.io/ciq312/fasternfaster-api`, `ghcr.io/ciq312/fasternfaster-frontend`), tagged `:latest` and `:${sha}`.
- `deploy`: SSH to the server, `cd /opt/Faster-n-Faster && git pull --ff-only && docker compose -f docker-compose.yml -f docker-compose.prod.yml pull && up -d && docker image prune -f`.
- Triggers on push/PR to `main`. Publish + deploy only run on push to `main`.

### Frontend Dockerfile

- Multi-stage: `node:22-alpine` → `npm ci && npm run build`, then `nginx:alpine` serving `/usr/share/nginx/html`. Exposes 3000. No Vite dev server in prod.

### Backend process

- Inside the `backend` container the app runs as **root**: `dotnet FasterNFaster.Api.dll`, PID 1 in the container.
- Framework-dependent deployment (uses the runtime in the base image, not self-contained).

### Known issues to fix (not yet done)

1. **Double reverse proxy for `/api/*` and `/gameHub`** — Caddy → frontend nginx → API. Should route directly from Caddy to `api:8080` and remove the proxy blocks from `fasternfasterapp/nginx.conf`. One less hop, simpler ops, more reliable WebSocket upgrades.
2. **API container runs as root** — should set a non-root `USER` in the API Dockerfile. Without this, an RCE in the app gets the attacker root inside the container (and possibly host escape via docker socket if mounted). Also blocks `dotnet-counters` from attaching without sudo gymnastics.
3. **No swap on the host** — 3.8 GB RAM with `Swap: 0B`. If memory pressure hits, OOM killer takes a process down. Add a 2 GB swapfile with `vm.swappiness=10` as a safety net.
4. **No DB migrations in deploy step** — once the schema starts changing, deploys will start booting against a stale DB. Add `dotnet ef database update` (or a migration-on-startup gate) to the deploy flow.
5. **No rollback path** — `:latest` tag is the only way back. Tagging with `${sha}` exists, so a manual rollback is `docker compose ... up -d` with an overridden image tag. Worth documenting an emergency runbook.
6. **`drop: ["console", "debugger"]` in `vite.config.js`** — strips *all* `console.*` calls in prod, including `.warn` and `.error`. Switch to `pure: ["console.log", "console.debug"]` so real errors still surface in the browser console / error trackers.
7. **`setInterval` for the latency check in `ConnectionProvider.jsx` is never cleared on unmount** — leaks an interval per remount (HMR, route changes, StrictMode). Return the id and `clearInterval` in the cleanup function.
8. **`UpdateRaceState` broadcasts likely too noisy** — `LobbyStateBroadcaster.BroadcastLobbyState` is called after progress updates. If it sends full lobby state to all players per keystroke, that's N² messages/sec. Should broadcast a slim per-player progress event and throttle (e.g. flush every 100ms).
9. **The hub echoes a player's own progress back to themselves** if broadcasts use `Clients.Group`. Use `Clients.OthersInGroup` for progress, since the typing player already knows what they typed.

### Diagnostic tooling on the server

- Use `dotnet-counters` and `dotnet-trace` **inside** the `backend` container (the API has PID 1 there). The host has no dotnet CLI on PATH for the `deploy` user.
- Quick way: `docker exec -it backend bash`, install `dotnet-counters` as a global tool, attach to PID 1.
- Counter to watch first under load: `threadpool-queue-length` in `System.Runtime`. Sustained > 0 indicates thread starvation (blocking sync calls on a hot path) and is the most common cause of latency spikes.
