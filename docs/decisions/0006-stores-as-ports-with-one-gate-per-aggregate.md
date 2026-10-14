# 0006. Stores as ports, one async gate per aggregate

- **Status:** Accepted
- **Date:** 2026-07-02 (semaphore), 2026-10-12 (store ports)
- **Sources:** [a00a2f3](https://github.com/ciq312/Faster-n-Faster/commit/a00a2f3) (replace reader-writer lock with semaphore to fix pool starvation), [cef6444](https://github.com/ciq312/Faster-n-Faster/commit/cef6444) and its revert [034b605](https://github.com/ciq312/Faster-n-Faster/commit/034b605), [#25](https://github.com/ciq312/Faster-n-Faster/pull/25) (remove empty lobbies atomically under the gate), [#31](https://github.com/ciq312/Faster-n-Faster/pull/31) (read lobby state only through the aggregate gate), [#40](https://github.com/ciq312/Faster-n-Faster/pull/40) (simplify lobby and race state behind store ports)

## Context

Many connections touch the same lobby at once: joins, leaves, kicks, color changes, the race tick, disconnect timers. Lobby and race objects aren't thread-safe, so every access needs to be serialized per aggregate.

Before #40, one in-memory dictionary of lobbies and one of races were reached through seven abstractions (`ILobbyAccess`, `ILobbyRepository`, `IPlayerLocationRegistry`, `ILobbyQuery`, `IRaceAccess`, …), and the two sides used different patterns. Live aggregates could leak out of the lock: `Lobby.Players` was read outside it and was publicly mutable (fixed in #31).

## Decision

- One store per aggregate type, behind a port in UseCases: `ILobbyStore` (`InMemoryLobbyStore`) and `IRaceStore` (`InMemoryRaceStore`).
- Each store owns its dictionary and an `AggregateGate<T>`: one `SemaphoreSlim(1, 1)` per aggregate id.
- **Mutations** run inside the gate (`Mutate`, `TryMutate`). Their domain events are dispatched after the gate is released ([0005](0005-domain-events-dispatched-after-the-gate.md)).
- **Reads** pass a projection that runs inside the gate (`Read`, `TryRead`). Only its result leaves the store, so no live aggregate ever does.
- The lobby store also owns the player → lobby and invite-code indexes and updates them as part of each mutation. "One lobby per player" is an atomic `GetOrAdd` claim on the player index (`TryClaimPlayer`).
- Removing an empty lobby is checked and done inside its gate (`RemoveIfEmpty`), so a join can't slip in between the check and the removal.

## How it works

`AggregateGate<T>` keeps a `ConcurrentDictionary<Guid, SemaphoreSlim>`, one semaphore per aggregate id, created on first use. Simplified:

```csharp
var gate = gates.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
await gate.WaitAsync();          // waits without blocking a thread
try   { return body(aggregate); } // synchronous: no await inside the gate
finally { gate.Release(); }
```

- `Mutate(id, action)` runs `action` plus `DrainEvents()` as the body, then dispatches the drained events after `Release` ([0005](0005-domain-events-dispatched-after-the-gate.md)).
- `Read(id, projection)` runs `projection` as the body and returns only its result, for example `l => l.GetPlayerIds()` or `r => r.GetSnapshot()`.

### Why reads take the gate too

The aggregates aren't thread-safe. `Lobby` keeps its players in a `List`, `Race` its participants in a `Dictionary`, and `RaceParticipant` has plain mutable fields. Reading while another call writes can throw ("collection was modified") or return a half-updated state, such as a new host who is no longer in the player list. So a read has to exclude writers.

It doesn't strictly have to exclude other readers: two projections running at once are safe as long as no write is in progress. Allowing that would take a reader/writer lock. It isn't done, for these reasons:

- **Contention is per aggregate, not global.** Two calls only wait for each other when they touch the same lobby at the same moment.
- **The critical sections are tiny.** A projection copies a few fields; there's no I/O and no `await` inside the gate.
- **A racing lobby is write-heavy.** Each player sends up to 10 progress updates a second (writes), against 5 tick reads a second per lobby. Writers stay exclusive under a reader/writer lock, so it would rarely let anything run in parallel.
- **.NET has no built-in async reader/writer lock.** The synchronous one was tried and caused thread pool starvation (see below).

No measurement has shown the gate to be a bottleneck. If one does, the better fix is copy-on-write: publish an immutable snapshot after each write and let readers take the latest snapshot without the gate. That costs an allocation per write, which suits lobbies (rare writes) but not races (constant writes).

## Alternatives considered

- **`ReaderWriterLockSlim`.** Replaced by the semaphore during load testing because it caused thread pool starvation: it's thread-affine and blocks the waiting thread, while `SemaphoreSlim.WaitAsync` frees the thread while it waits. It was tried once more the same day and reverted.
- **An async reader/writer lock** (for example from Nito.AsyncEx). Adds a dependency and per-call overhead for little parallelism, as above.
- **Copy-on-write snapshots for reads.** The candidate if the gate ever shows up in profiling, starting with lobbies.
- **One global lock.** Not recorded as considered; it would serialize every lobby behind every other.
- **Keep the separate access, query and location interfaces.** Removed in #40 as speculative: more types for the same behaviour.

## Consequences

- Each lobby and race is single-threaded, while different lobbies run in parallel.
- Handlers can't hold a reference to a live aggregate between calls; they re-read through the store.
- The gate isn't re-entrant: code inside a mutation must not call back into the same store for the same id.
- Gates are removed but deliberately not disposed, because concurrent callers may still be waiting on them (comment in `AggregateGate.Release`).
- Another store implementation, such as a Redis one for scaling out, only has to honour the port's contract.
