# 0005. Domain events as MediatR notifications, dispatched after the gate

- **Status:** Accepted
- **Date:** 2026-06-27
- **Sources:** [2fa1865](https://github.com/ciq312/Faster-n-Faster/commit/2fa1865) (remove MediatR dependency from Core), [30419b2](https://github.com/ciq312/Faster-n-Faster/commit/30419b2) (dispatch domain events dynamically instead of double dispatch), [d82570d](https://github.com/ciq312/Faster-n-Faster/commit/d82570d) (dispatch aggregate events under lock), [598a9fd](https://github.com/ciq312/Faster-n-Faster/commit/598a9fd) (remove the RaceSessionEndedEvent hop), [#40](https://github.com/ciq312/Faster-n-Faster/pull/40)

## Context

Things that happen in the domain (a player joins, finishes, breaks a typing rule; a race ends) trigger several side effects: SignalR messages, saving results, sanctions, preparing the next race. Calling those from the aggregate would tie the domain to SignalR and the database. Calling them all from one handler would make that handler grow with every new side effect.

## Decision

- Aggregates raise domain events (`PlayerJoinedEvent`, `RaceFinishedEvent`, `RaceViolationEvent`, …) into a list on `AggregateRoot`.
- `AggregateGate` drains the list inside the aggregate's gate and dispatches the events after releasing it ([0006](0006-stores-as-ports-with-one-gate-per-aggregate.md)).
- `MediatREventDispatcher` wraps each event in `DomainEventNotification<T>` and publishes it. The wrapper's type is built from the event's runtime type with `dynamic`, so events don't need a `Dispatch` method of their own.
- Each side effect is its own notification handler. `RaceFinishedEvent`, for example, has three: broadcast the results, queue them for saving, and end the lobby session and load the next passage.

The principle is the **open-closed principle**: a new side effect is a new handler, and the aggregate and the existing handlers stay untouched. The domain only knows its own `IEventDispatcher` interface; the MediatR implementation lives in UseCases (dependency inversion, [0003](0003-clean-architecture-over-layered.md)).

## How it works

The host disconnects:

```
DisconnectHandler
  └─ lobbies.TryMutate(lobbyId, l => l.Disconnect(playerId))
       gate.WaitAsync()
         Lobby.Disconnect
           remove player, promote next host
           RaiseDomainEvent(PlayerRemovedEvent)        ← recorded on the aggregate,
           RaiseDomainEvent(PlayerDisconnectedEvent)     nothing runs yet
         events = lobby.DrainEvents()
       gate.Release()
       foreach event → MediatREventDispatcher.Dispatch
         PlayerRemovedEvent      → CleanupEmptyLobbyHandler           (remove the lobby if empty)
         PlayerDisconnectedEvent → BroadcastPlayerDisconnectedHandler (tell the others)
```

The aggregate decides what happened. Handlers decide what to do about it, and run only after the state change is complete and the gate is free.

## Alternatives considered

- **Double dispatch,** where each event calls back into the dispatcher with its own type. Rejected because it forces a method onto every event (comment in `MediatREventDispatcher`).
- **Dispatching inside the gate.** Tried in [d82570d](https://github.com/ciq312/Faster-n-Faster/commit/d82570d); today events go out after release. The gate isn't re-entrant and handlers read the same lobby again (the broadcaster resolves a lobby's members through the store), so dispatching inside it would deadlock.

## Consequences

- The domain doesn't reference SignalR or MediatR.
- A new side effect is a new handler; existing code stays untouched.
- Handlers run after the state change is visible to others, so they must not assume nothing else happened in between.
- The flow of a single action is spread across several handlers; finding them means searching for the event's notification type.
