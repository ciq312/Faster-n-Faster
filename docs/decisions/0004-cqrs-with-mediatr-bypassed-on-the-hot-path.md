# 0004. CQRS with MediatR, bypassed on the keystroke hot path

- **Status:** Accepted
- **Date:** 2026-03-17, read models split 2026-08-13, hot-path bypass since 2026-07-02
- **Sources:** [509d1fc](https://github.com/ciq312/Faster-n-Faster/commit/509d1fc) (first use case), [cac6c16](https://github.com/ciq312/Faster-n-Faster/commit/cac6c16) (remove redundant lobby service facade), [4b987c0](https://github.com/ciq312/Faster-n-Faster/commit/4b987c0) (separate read and write models), [e49baa5](https://github.com/ciq312/Faster-n-Faster/commit/e49baa5) (bypass MediatR on race update hot path)

## Context

Each player action is a small, separate piece of behaviour: join a lobby, kick a player, start a race, log in. Earlier code grouped them in large services (`LobbyService`) that every new feature had to edit. Reads and writes also have different shapes: the leaderboard needs sorted, paged rows across all players, while a write needs one aggregate and its invariants.

## Decision

### Commands and queries are separate, and so are their models

This is CQRS at the model level: one database and one in-memory store, but separate paths and separate models for changing state and for reading it.

- **Commands** (`JoinLobbyCommand`, `KickPlayerCommand`, …) change state. Their handlers load an aggregate through a store or repository, call its methods, and let it enforce its own invariants.
- **Queries** (`GetLeaderboardQuery`, `GetLobbiesQuery`) never return aggregates. Each returns a read model shaped for its screen, built by its own path:
  - The leaderboard is a no-tracking SQL projection straight over the statistics table into `LeaderboardEntry`. It never loads a `User`.
  - The lobby list projects each lobby into a `LobbyListItem` inside the lobby's gate.
  - `LobbyState` is built by its own read port, `ILobbyQuery`.

There is no separate read database and no event sourcing: reads see writes immediately. A separate read store would only pay off if reads and writes needed to scale independently, which a single instance doesn't ([0002](0002-live-game-state-in-memory-on-one-instance.md)).

### MediatR carries the requests

- An endpoint or hub method builds a command and calls `sender.Send(command)`. It doesn't know which class handles it; MediatR finds the one handler registered for that request type. This is the mediator pattern: senders and handlers only know the message.
- Each handler does one use case. **New behaviour is a new handler, not an edit to a shared service** (open-closed principle).
- Cross-cutting behaviour wraps handlers as a pipeline behavior, applied by request type, without the handler knowing. `LobbyStateFlushBehavior` wraps every command that implements `ILobbyStateRequest` ([0008](0008-one-lobbystate-broadcast-per-changed-lobby.md)).

### The one exception: progress updates

`GameHub.UpdateRaceState`, called up to ten times a second per racing player, calls `IRaceStore.TryMutate` directly. The bypass was added during load testing on 2026-07-02; the comment at the call site gives the reason: to minimize allocations.

## Alternatives considered

- **Service classes with one method per action,** as the code started. Every feature edited the same large class; they were removed.
- **Reading through aggregates.** Loading users to build a leaderboard, or returning live lobbies to callers, would couple screens to the write model and, for lobbies, leak state out of the gate ([0006](0006-stores-as-ports-with-one-gate-per-aggregate.md)).
- **Sending progress through MediatR like everything else.** Consistent, but adds the mediator's allocations to the busiest call in the system.

## Consequences

- Handlers are small and tested one at a time.
- Read models can change for a screen without touching aggregates, and the other way round.
- The hot path skips every pipeline behavior. Anything that must apply to progress updates, such as validation or sanctions, has to live in the domain (`RaceParticipant`) or in hub filters, not in a MediatR behavior.
