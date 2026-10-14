# 0002. Live game state in memory on one API instance

- **Status:** Accepted
- **Date:** 2026-03-17
- **Sources:** [62bdbd4](https://github.com/ciq312/Faster-n-Faster/commit/62bdbd4) (SignalR connection and core entities), [2d786f8](https://github.com/ciq312/Faster-n-Faster/commit/2d786f8) and [7fe5734](https://github.com/ciq312/Faster-n-Faster/commit/7fe5734) (lobby and race decoupled), [#40](https://github.com/ciq312/Faster-n-Faster/pull/40) (stores behind ports)

## Context

During a race every player sends progress up to ten times a second, and the server broadcasts race state five times a second per lobby ([0007](0007-race-tick-with-latest-frame-conflation.md)). Lobby and race state change constantly, matter only while the lobby exists, and lose little if they disappear.

## Decision

Lobbies and races live in the API's memory, in `InMemoryLobbyStore` and `InMemoryRaceStore`. Postgres keeps only durable data (users, statistics, results, bans) and Redis keeps tokens and caches. The API runs as a single instance.

## Alternatives considered

- **Redis-backed lobby and race state from the start.** Every keystroke would cost a network round-trip and a serialization. The outline of this design is kept for when it's needed (see [Scaling out](../architecture.md#scaling-out-later)).

## Consequences

- The hot path has no network I/O, which keeps latency and cost low on one small VPS.
- Restarting or deploying the API ends every running lobby and race.
- No horizontal scaling. Other in-memory pieces depend on this too: sessions, strikes, pending reconnect removals, the race tick registry and the connection terminator.
- The stores sit behind ports (`ILobbyStore`, `IRaceStore`), so moving to Redis replaces the adapters, not the handlers ([0006](0006-stores-as-ports-with-one-gate-per-aggregate.md)).
