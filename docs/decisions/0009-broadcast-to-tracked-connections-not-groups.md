# 0009. Broadcast to tracked connections, not SignalR groups

- **Status:** Accepted
- **Date:** 2026-07-01
- **Sources:** [bb81edf](https://github.com/ciq312/Faster-n-Faster/commit/bb81edf) (map lobby users to connections without SignalR groups), [b8ed9ab](https://github.com/ciq312/Faster-n-Faster/commit/b8ed9ab) (introduce IBroadcaster for UseCases handlers), [1d35db9](https://github.com/ciq312/Faster-n-Faster/commit/1d35db9) (broadcast to a client list instead of one by one), [#28](https://github.com/ciq312/Faster-n-Faster/pull/28) (clear only the disconnecting connection's session)

## Context

Handlers in UseCases need to message "everyone in this lobby", "everyone except this player", "this player" or "these players", without referencing SignalR ([0003](0003-clean-architecture-over-layered.md)). SignalR's built-in answer is groups, but group membership is per connection and is a second membership list next to the lobby store's own.

## Decision

- UseCases sends through the `IBroadcaster` port with an audience: `Audience.Lobby`, `LobbyExcept`, `Player`, `Players`.
- `SignalRBroadcaster` resolves the audience to user ids. For lobby audiences it reads the members from `ILobbyStore` through the gate. It then maps user ids to connection ids through `ISessionService` and sends once with `Clients.Clients(list)`.
- Each user has one active connection. A new connection replaces the old one, which receives `AnotherSessionStarted`.

## Alternatives considered

- **SignalR groups.** With groups, every join, leave, kick and reconnect has to update two lists, the lobby and the group, and they can disagree. Resolving from the store keeps the lobby the only source of truth.
- **One `SendAsync` per player.** Replaced with one call over a client list during load testing.

## Consequences

- Lobby membership exists in one place, the lobby store.
- A lobby broadcast takes the lobby's gate briefly to read its members, which is one reason events go out after the gate is released ([0005](0005-domain-events-dispatched-after-the-gate.md)).
- One connection per user: opening the game in a second tab takes over from the first.
- The session map is per instance; scaling out needs a backplane and a shared session store (see [Scaling out](../architecture.md#scaling-out-later)).
