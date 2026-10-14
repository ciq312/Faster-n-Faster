# 0008. One `LobbyState` broadcast per changed lobby

- **Status:** Accepted
- **Date:** 2026-09-20, decorator since 2026-10-12
- **Sources:** [014b91f](https://github.com/ciq312/Faster-n-Faster/commit/014b91f) (replace lobby state broadcaster with generic broadcaster), [ebacdab](https://github.com/ciq312/Faster-n-Faster/commit/ebacdab) (broadcast LobbyState once per lobby state change), [#40](https://github.com/ciq312/Faster-n-Faster/pull/40)

## Context

`LobbyState` is the full picture of a lobby (players, host, colors, settings, session state), and clients redraw the lobby from it. One player action often changes a lobby in several ways and raises several domain events. When the host disconnects, for example, the player is removed, the next player becomes host, and `PlayerRemovedEvent` and `PlayerDisconnectedEvent` fire, each with its own handlers.

## Decision

`LobbyState` is sent by one mechanism, once per changed lobby, when the whole command has finished. Handlers never send it.

## How it works

Three pieces, wired together by MediatR and DI:

- `ILobbyStateRequest`, a marker on every command that can change a lobby (`JoinLobbyCommand`, `DisconnectCommand`, `KickPlayerCommand`, `StartRaceCommand`, …).
- `TrackingLobbyStore`, a decorator registered as `ILobbyStore` around `InMemoryLobbyStore`. After every successful `Mutate`/`TryMutate`, it marks that lobby as changed.
- `LobbyStateFlushBehavior`, a MediatR pipeline behavior that runs every `ILobbyStateRequest` inside a `LobbyStateScope`.

The loop for one command:

```
sender.Send(DisconnectCommand)
└─ LobbyStateFlushBehavior → LobbyStateScope.Run
     tracker.TryBeginScope()                    changed = { }   (an AsyncLocal set)
     ├─ DisconnectHandler
     │    lobbies.TryMutate(L, l => l.Disconnect(p))
     │       → player removed, host promoted     (TrackingLobbyStore marks L)
     │                                           changed = { L }
     │    events dispatched after the gate:
     │       PlayerRemovedEvent      → CleanupEmptyLobbyHandler
     │       PlayerDisconnectedEvent → BroadcastPlayerDisconnectedHandler
     │       (a handler that mutates L marks it again — the set keeps one L)
     tracker.EndScope()                         → [ L ]
     for each lobby in [ L ] that still exists:
         state = ILobbyQuery.GetLobbyState(L)   (read once, after everything above)
         broadcast LobbyState to L               ← exactly one message
```

Details that make it hold together:

- **The set de-duplicates.** However many mutations and events touch lobby `L` during the command, `L` is in the set once, so it gets one `LobbyState`.
- **Event handlers are inside the scope.** Domain events are dispatched inside the store call, and `AsyncLocal` flows down the async call tree, so changes made by event handlers land in the same set as the command's own changes. This relies on the tracker being a singleton: event handlers run in their own DI scope, and a scoped tracker would hold a different `AsyncLocal`.
- **Only the outermost scope flushes.** `TryBeginScope` returns false if a scope is already open, so a command sent from inside another command joins the outer set.
- **The state is read at the end.** The broadcast carries the lobby as it is after the command and all its handlers finished, never an intermediate state.
- **Nothing is sent if the command throws.** The scope ends in a `finally`, but the broadcast runs only after the command returns. Aggregate methods check their rules before changing state, so a failed command normally has nothing to report.
- **Two exceptions:** `RefreshHandler` marks the lobby explicitly, to re-send state without changing it. `RaceFinishedOrchestrationHandler` is a notification handler, not a command, so it opens the scope itself.

## Why

- **One update per action, not one per change.** The host-disconnect case above changes the player list and the host and raises two events, but clients redraw once, with the final state.
- **Handlers don't know broadcasting exists** (single responsibility). A handler changes the lobby; whether and when clients hear about it is decided in one place. A new lobby command gets correct broadcasting by implementing the marker, without any broadcast code (open-closed).
- **It works for any store.** Marking sits in a decorator over the `ILobbyStore` port, so a future Redis store gets it without changes ([0006](0006-stores-as-ports-with-one-gate-per-aggregate.md)).

## Alternatives considered

- **Each handler broadcasts `LobbyState` itself,** which was the earlier approach. A command whose work is split across the handler and several event handlers ends up sending the state more than once, or sending it before the last change.
- **Broadcast from inside the store on every mutation.** Same duplication, and it would broadcast intermediate states.

When the lobby state code was simplified (#40), the mechanism was reviewed and kept.

## Consequences

- A command sends at most one `LobbyState` per lobby it changed.
- The rule to follow: a command that changes a lobby must implement `ILobbyStateRequest`, or run inside another scope. Outside a scope, `MarkChanged` has nowhere to record the lobby, so no `LobbyState` is sent.
- Coalescing is per command, not per time window: two commands in quick succession still send two updates.
