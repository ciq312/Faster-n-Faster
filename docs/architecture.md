# Architecture

How Faster'n'Faster is put together at runtime and why. The README has the short version (request flow and backend layers); this document goes through where state lives, how the main flows run, and the concurrency model. Each design choice links to its record in [`decisions/`](decisions/README.md).

## Runtime topology

```
                 ┌───────────────────────── VPS (Docker Compose) ─────────────────────────┐
Browser ──HTTPS──▶ Caddy :443 ──/api/*, /gameHub──▶ api :8080 (ASP.NET Core, one instance)  │
                 │    │                              │        │          │                  │
                 │    └──everything else──▶ frontend :3000    ▼          ▼                  │
                 │       (nginx, built React SPA)   PostgreSQL 15   Redis 7                 │
                 └──────────────────────────────────────────────────────────────────────────┘
                                                     api ──▶ Google OAuth, Resend (SMTP)
```

| Container | Role |
|---|---|
| `caddy` | TLS (Let's Encrypt), security headers, routes `/api/*` and `/gameHub` to the API and everything else to the frontend. The hub route has a 24 h read timeout so WebSockets aren't cut. |
| `frontend` | nginx serving the built React SPA. No server-side rendering. |
| `backend` (api) | REST endpoints (FastEndpoints) and the SignalR hub. Holds all live lobby and race state in memory. |
| `postgresDB` | Durable data. |
| `redis` | Tokens and the leaderboard cache. |

Images are built in CI, pushed to GHCR and deployed pinned to the commit SHA. The pipeline itself is described in the README's CI/CD section.

## Where state lives

| Data | Where | Survives an API restart? |
|---|---|---|
| Users, external logins, statistics, race results, bans and suspensions | PostgreSQL (EF Core, `AppDbContext`) | Yes |
| Refresh tokens, email-verification and password-reset tokens | Redis, with TTLs | Yes |
| Leaderboard pages | Redis, 60 s cache-aside; the player's own rank (`/api/leaderboards/me`) is read live | Yes (it's a cache) |
| Lobbies, races, player → lobby index, invite codes | API memory: `InMemoryLobbyStore`, `InMemoryRaceStore` ([0002](decisions/0002-live-game-state-in-memory-on-one-instance.md)) | No |
| Active connection per user, strikes, pending reconnect removals, racing-lobby registry, sanction terminator's connection list | API memory (singletons) | No |
| Finished race results waiting to be written | In-process channel, `RaceResultQueue` | No |

A restart therefore ends every running lobby and race. Accounts, statistics and sessions (refresh tokens) are unaffected.

## Backend structure

Clean Architecture: four projects, dependencies pointing inward, `Presentation → Infrastructure → UseCases → Domain`. UseCases declares the interfaces it needs (ports); Infrastructure and Presentation implement them. That inversion is what separates it from a traditional layered design ([0003](decisions/0003-clean-architecture-over-layered.md)). Inside each project, folders follow the same feature areas — `Auth`, `Users`, `Lobbies`, `Races`, `AntiCheat`, `Realtime`, `Leaderboards`, plus `Shared` — so each use case's command and handler sit together. The folder tree is in the README.

| Project | Holds | Must not reference |
|---|---|---|
| `Domain` | Aggregates (`Lobby`, a single sealed `Race`, `User`, …), their invariants and domain events; domain exceptions | Anything outside the BCL |
| `UseCases` | Commands/queries and handlers per use case, `Ports/` interfaces per area, application services, the aggregate gate | EF Core, Redis, SignalR, ASP.NET |
| `Infrastructure` | Adapters behind the ports: Postgres repositories, Redis stores, SMTP, in-memory lobby/race stores, the race tick and conflator, background services | Presentation |
| `Presentation` | FastEndpoints endpoints, `GameHub` and hub filters, the SignalR broadcaster, exception → status-code mapping, DI composition root | — |

## Main flows

### A command over HTTP or the hub

Most actions — create/join/leave a lobby, start a race, kick, change color, log in — follow the same path ([0004](decisions/0004-cqrs-with-mediatr-bypassed-on-the-hot-path.md)):

1. An endpoint or a `GameHub` method builds a command and sends it through MediatR.
2. For lobby commands, `LobbyStateFlushBehavior` opens a scope that collects which lobbies change ([0008](decisions/0008-one-lobbystate-broadcast-per-changed-lobby.md)).
3. The handler mutates an aggregate through a store port. `TrackingLobbyStore` marks the lobby as changed; `InMemoryLobbyStore` runs the mutation inside that lobby's gate ([0006](decisions/0006-stores-as-ports-with-one-gate-per-aggregate.md)).
4. After the gate is released, the aggregate's domain events are published as MediatR notifications ([0005](decisions/0005-domain-events-dispatched-after-the-gate.md)). Handlers in `UseCases/Realtime/` turn them into targeted SignalR messages (`PlayerJoined`, `PlayerKicked`, …) through the `IBroadcaster` port, which resolves players to their connections ([0009](decisions/0009-broadcast-to-tracked-connections-not-groups.md)).
5. When the command finishes, one `LobbyState` is broadcast per changed lobby.

Errors: a `StatusException` from the domain or a handler becomes a status code over HTTP, or a `HubException` with the message over the hub ([0010](decisions/0010-http-status-codes-mapped-in-presentation.md)). Every HTTP error body, including validation failures and unexpected 500s, is `{ "reason", "code", "errors" }` (see the README's API section).

### Keystroke progress (the hot path)

1. The client renders typing locally and sends a progress snapshot `(index, mistakes, typed)` at most every 100 ms; intermediate snapshots are dropped (latest wins).
2. `GameHub.UpdateRaceState` finds the player's lobby from the store's index and calls `IRaceStore.TryMutate` directly — no MediatR on this path ([0004](decisions/0004-cqrs-with-mediatr-bypassed-on-the-hot-path.md)).
3. `RaceParticipant` checks the snapshot against the passage: the typed prefix must match, overflow past the last correct character is capped at 6, and no space may follow a wrong character. A broken rule rejects the update and raises `RaceViolationEvent`, which feeds the sanction ladder (race withdrawal, then strikes, then a 24 h suspension for registered players). New characters are then granted from a typing budget (350 WPM plus 6 characters of slack); anything beyond it is held as a pending claim and granted on later ticks as the budget refills ([0001](decisions/0001-server-authoritative-race-progress.md)).
4. Nothing is broadcast here. Progress only lands in memory.

Snapshots are planned to give way to batched keystroke events that the server replays ([0011](decisions/0011-batched-keystroke-events-replayed-on-the-server.md)).

### Race tick

`RaceTickService` runs every 200 ms over the lobbies registered in `RaceTickRegistry` ([0007](decisions/0007-race-tick-with-latest-frame-conflation.md)):

- **Countdown:** once the countdown and start delay have passed, the race starts and `RaceStarted` goes to the lobby.
- **Racing:** it takes a snapshot of every participant and hands it to `RaceStateConflator`, which keeps one latest frame per lobby and sends it as `RaceState` to that lobby's players. A frame that is still waiting when the next tick arrives is replaced, not queued.

### Race end

When the last participant finishes, `Race` raises `RaceFinishedEvent` and resets. Three independent handlers react to it ([0005](decisions/0005-domain-events-dispatched-after-the-gate.md)):

- `BroadcastRaceFinishedHandler` sends the results to the lobby.
- `SaveRaceResultHandler` enqueues them on `RaceResultQueue`; `RaceResultProcessor`, a background service, writes statistics to Postgres off the hub call.
- `RaceFinishedOrchestrationHandler` stops the lobby's tick, ends the lobby session and loads the next passage.

### Disconnects and reconnects

- A player stays in their lobby while they have an active hub connection anywhere in the app, not only on the lobby page ([0013](decisions/0013-lobby-membership-follows-the-active-connection.md)).
- `OnDisconnectedAsync` starts a 15 s grace period (`FastReconnectHandler`). Any new hub connection cancels it (`ReconnectCommand` from `OnConnectedAsync`). When it ends, `DisconnectCommand` removes the player only if they have no active connection (`ISessionService.GetActiveSession`), so a stale connection that is detected late can't remove a player who is already back.
- A disconnected racer's cursor freezes; the race goes on without them and they're left out of the results.
- If the host leaves, the next player in the lobby is promoted.
- Empty lobbies are removed atomically under their gate. Lobbies that were created but never joined are swept by `EmptyLobbySweepService` (TTL 2 min, sweep every minute).
- Each user has one active connection. A second connection takes over and the first receives `AnotherSessionStarted`. The old tab navigates to the start page, which closes its connection.

### Authentication

- Login (email/password, Google OAuth, or guest) issues an RS256 access JWT (5 min; 7 days for guests) and an opaque refresh token (4 h, rotated on use), both in HttpOnly cookies.
- The JWT bearer handler reads the access token from its cookie, so REST calls and the hub connection authenticate the same way.
- On a 401 the client refreshes once — concurrent requests share one in-flight refresh — and retries.
- A banned or suspended player can't open a hub connection; a player sanctioned while connected has their connection aborted.

## Concurrency model

- **One gate per aggregate.** Every lobby and every race has its own `SemaphoreSlim(1, 1)` in `AggregateGate<T>`. Mutations run inside it; reads run a projection inside it and return the result, so a live aggregate never leaves its store ([0006](decisions/0006-stores-as-ports-with-one-gate-per-aggregate.md)).
- **Events after release.** Domain events are drained inside the gate and dispatched after it is released. The gate isn't re-entrant, and handlers routinely read the same lobby again (the broadcaster resolves a lobby's members through the store), so dispatching inside it would deadlock.
- **Indexes live with the store.** `InMemoryLobbyStore` keeps the player → lobby and invite-code indexes in sync with each mutation. "One lobby per player" is an atomic `GetOrAdd` claim on that index.
- **Lock-free broadcasting.** The conflator swaps frames with `Interlocked` and runs at most one send loop per lobby.
- **Off-path persistence.** Race results go through a single-reader channel, so a slow database never blocks a hub call.

## Frontend

- React 19 + Vite SPA with react-router, in TypeScript (strict `tsconfig.json`, checked by `tsc` in CI). Code is grouped by feature: `features/{auth, connection, game, lobbies, leaderboard, profile}`, routed screens in `pages/`, the Redux store in `app/`, shared components and utilities in `shared/`.
- One SignalR connection, owned by a `GameHub` port (`features/connection/gameHub.ts`) whose adapter (`signalRGameHub.ts`) is the only code that imports `@microsoft/signalr`. It is the thunk extra argument (`setupStore({ hub })`), so hub calls are thunks and tests pass a fake hub (dependency inversion). Each server event becomes one typed action (`hubEvents`), registered once per connection; reactions (banners, stopping the hub on ban/suspension/another session, error banners for failed hub calls, disconnect on logout, throttling race progress to one `UpdateRaceState` per 100 ms with the final one sent at once) are listener-middleware listeners in `app/listenerMiddleware.ts`.
- The `connection` slice holds the status and why a session ended; `ConnectionLayout` connects on mount, disconnects on unmount and navigates home when the session ends. The `lobby` slice keeps the last `LobbyState`; selectors expose it only once the current user is known and in it, so the navbar's "back to lobby" works on any page once a `LobbyState` has arrived. Banners are a `banner` slice rendered by `BannerHost`.
- The `race` slice is the race's state machine, `idle → countdown → racing → finished`, one state variant per phase, so a countdown can't enable typing and results can't coexist with a running race. `LobbyState`'s `isRaceStarting`/`isRaceStarted` restore the phase after a reload; `RaceState` applies only while racing, and the withdrawal flag lives only in the racing variant. The countdown number ticks inside the `Countdown` component; typed text stays in `useTyping`, and `Lobby` dispatches `progressTyped` for the throttle listener.
- REST goes through one RTK Query api (`shared/api/api.ts`) on `baseQueryWithReauth`, which does the refresh-and-retry above through the shared single-flight `refreshOnce`. Features add endpoints with `injectEndpoints`; login opts out of the refresh because its 401 means wrong credentials. Creating a lobby invalidates the `Lobbies` tag, so a mounted lobby list refetches.
- The current user is the `me` query cache read through selectors (`features/auth/authSelectors.ts`), with no separate auth state. Login and guest invalidate it, a successful logout resets the api state.
- `useTyping` applies the typing rules locally for instant feedback; the server re-checks them ([0001](decisions/0001-server-authoritative-race-progress.md)).

## Testing

- **Unit tests** (`FasterNFaster.UnitTests`): xUnit with hand-rolled fakes and `FakeTimeProvider`; no mocking framework.
- **Integration tests** (`FasterNFaster.IntegrationTests`): real PostgreSQL and Redis through Testcontainers, one host per test.
## Scaling out (later)

The API runs as a single instance on purpose ([0002](decisions/0002-live-game-state-in-memory-on-one-instance.md)). Running several would need:

- **A Redis SignalR backplane,** so a broadcast reaches players connected to other instances.
- **A `RedisLobbyStore` behind `ILobbyStore`.** Lobbies are serialized to Redis, and a distributed lock or optimistic versioning replaces the in-process gate. The player → lobby and invite-code indexes become Redis keys, and the player claim becomes `SET NX`. Handlers only talk to the port and LobbyState marking sits in the decorator over it, so they don't change.
- **Race state pinned to one instance.** The keystroke hot path stays in memory on the instance that owns the race. The lobby records the owner, and that instance's tick loop broadcasts, so there's no Redis round-trip per keystroke.
- **Shared or sticky per-instance registries.** Strikes (`StrikeRegistry`), pending reconnect removals (`PendingRemovalRegistry`), sessions (`InMemorySessionService`), the race tick registry and the connection terminator are in-process today.

## Decisions

All architecture decisions, with their context, the alternatives that were weighed and the consequences, are in [`decisions/`](decisions/README.md).
