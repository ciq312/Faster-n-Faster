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
| Ephemeral / pub-sub | Redis (also SignalR backplane) |

## Architecture

- **Server-authoritative typing validation**: The client only renders visual feedback locally. Every keystroke index is sent to the server, which validates it against the source passage before updating Redis and broadcasting progress. Never trust client-reported progress.
- **Live game state in Redis**: Player positions and progress percentages live in Redis (ephemeral). PostgreSQL stores lobby metadata, completed race results, and `comment_thresholds`.
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
- **Redis down**: fail gracefully — race unavailable, no silent data corruption.
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
Controllers/   — HTTP endpoints. Thin — validate input, call services, return DTOs.
```

### Rules to Follow

1. **Dependencies flow inward.** Controllers/Hubs → Services → Interfaces ← Data. Never the reverse. A service must not reference a controller. Data implements interfaces but is never referenced by services directly.

2. **Controllers and Hubs are thin.** They handle HTTP/SignalR concerns (request parsing, validation, response shaping) and delegate to services. No business logic in controllers or hubs. If a method has an `if` that isn't about validation or response codes, it belongs in a service.

3. **Services contain business logic.** "If the lobby is private, generate an invite code" lives in a service (or the entity itself), not in a controller. Services depend on interfaces (`IRepository<T>`), not on `AppDbContext` directly.

4. **Entities own their invariants.** Use `private set` on properties. If an entity has rules about its own state (e.g., "status can only transition from waiting → racing → finished"), enforce them inside the entity with methods. Don't let outside code set properties freely.

5. **Use DTOs at API boundaries.** Never return an entity directly from a controller. Map to a DTO that contains only what the client needs. This decouples your database schema from your API contract.

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

- Local Redis via Docker for development.
- Environment config via `.env`: DB connection strings, Redis URL, CORS origins.
- Docs are in `Faster'n'FasterDocs/General/` (Obsidian vault) — `spec.md` is the authoritative spec.
