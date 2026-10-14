# 0003. Clean Architecture instead of a traditional layered architecture

- **Status:** Accepted
- **Date:** 2026-06-27, separate projects since 2026-08-01
- **Sources:** [5e6a264](https://github.com/ciq312/Faster-n-Faster/commit/5e6a264) (move interfaces from Infrastructure to UseCases), [2fa1865](https://github.com/ciq312/Faster-n-Faster/commit/2fa1865) (remove MediatR dependency from Core), [b3a2bbc](https://github.com/ciq312/Faster-n-Faster/commit/b3a2bbc) (fix dependency direction between UseCases and Infrastructure), [b8ea3ae](https://github.com/ciq312/Faster-n-Faster/commit/b8ea3ae) (split layers into separate projects)

## Context

The backend started as a traditional layered design: presentation calls business logic, business logic calls data access. The interfaces for repositories and services were defined next to their implementations in Infrastructure, so the use cases depended on Infrastructure, and through it on EF Core and Redis. The core also referenced MediatR.

The difference between the two designs is the direction of one arrow:

```
Traditional layered                     Clean Architecture

Presentation                            Presentation ──────────┐ (composition root:
     │                                       │                 │  wires adapters to ports)
     ▼                                       ▼                 ▼
UseCases ─────────▶ Infrastructure      UseCases ◀──────── Infrastructure
     │               (EF Core, Redis)        │   implements     (EF Core, Redis,
     ▼                                       ▼   the ports       in-memory stores)
Domain                                  Domain
```

In the layered version, business code compiles against persistence. In the clean version, persistence compiles against business code.

## Decision

Clean Architecture: every source dependency points inward, toward the domain.

The mechanism is the **Dependency Inversion Principle**. The code that needs something owns the interface for it:

- A use case that needs to store lobbies declares `ILobbyStore` in `UseCases/Lobbies/Ports/`. It says what the use case needs, in the use case's own terms.
- Infrastructure implements it (`InMemoryLobbyStore`). Infrastructure references UseCases, never the reverse.
- The same goes for outgoing messages: `IBroadcaster` and `IConnectionTerminator` are UseCases ports, implemented in Presentation by SignalR classes.
- Presentation is the composition root: the only place that knows which implementation backs which port.

Four projects make the rule a compile error rather than a convention:

| Project | Holds | References |
|---|---|---|
| `Domain` | Aggregates, invariants, domain events, domain exceptions | nothing outside the BCL |
| `UseCases` | Handlers and the ports they need | `Domain`, MediatR |
| `Infrastructure` | Adapters: Postgres, Redis, SMTP, in-memory stores, background services | `UseCases` |
| `Presentation` | Endpoints, the hub, SignalR adapters, DI wiring | `Infrastructure` (and through it, everything) |

## Why not stay layered

What the inversion buys in this app, concretely:

- **The contract is shaped by the use case, not by the technology.** `ILobbyStore` describes what lobby use cases need (mutate under a gate, read a projection, claim a player). It doesn't mention dictionaries. So the scale-out plan can put a Redis store behind the same port ([0002](0002-live-game-state-in-memory-on-one-instance.md)) and no handler changes. When the interface lived in Infrastructure next to its implementation, it was shaped by that implementation, and UseCases pulled in Infrastructure and its packages.
- **Use cases are testable without infrastructure.** Each port has a small hand-rolled fake, so handlers are unit-tested with no database, Redis or SignalR.
- **SignalR is a detail too.** Handlers say "send this to the lobby" through `IBroadcaster` ([0009](0009-broadcast-to-tracked-connections-not-groups.md)); they don't know hubs or connection ids exist.
- **The domain stays pure.** No EF, JSON or HTTP attributes in it ([0010](0010-http-status-codes-mapped-in-presentation.md)).

## Alternatives considered

- **Traditional layered architecture**, where the code started. Fewer interfaces and less indirection, but business code depends on persistence details, can't be tested without them, and changes when they change.
- **One project with layer folders.** The interim step. It had the right shape, but nothing stopped a handler from referencing EF Core or SignalR; separate projects turn that into a build error.

## Consequences

- More interfaces and more projects to navigate than a layered app.
- A port is shaped by what the use case needs, so an adapter sometimes has to translate (for example, the lobby store returns projections, not aggregates).
- MediatR is the one framework the application layer depends on ([0004](0004-cqrs-with-mediatr-bypassed-on-the-hot-path.md)). Domain has none.
