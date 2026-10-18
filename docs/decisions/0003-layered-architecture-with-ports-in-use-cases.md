# 0003. Layered architecture with ports in UseCases

- **Status:** Accepted
- **Date:** 2026-06-27, separate projects since 2026-08-01
- **Sources:** [5e6a264](https://github.com/ciq312/Faster-n-Faster/commit/5e6a264) (move interfaces from Infrastructure to UseCases), [2fa1865](https://github.com/ciq312/Faster-n-Faster/commit/2fa1865) (remove MediatR dependency from Core), [b3a2bbc](https://github.com/ciq312/Faster-n-Faster/commit/b3a2bbc) (fix dependency direction between UseCases and Infrastructure), [b8ea3ae](https://github.com/ciq312/Faster-n-Faster/commit/b8ea3ae) (split layers into separate projects)

## Context

The backend started as one project. To implement the project faster but it was decided to move to compile enforced version with separate project that reference other so that dependencies flow inward

## Decision

Four projects, with dependencies pointing inward:

```
Presentation → Infrastructure → UseCases → Domain
```

- **Domain** holds aggregates, their invariants, domain events and domain exceptions. It references nothing outside the BCL.
- **UseCases** holds handlers and the interfaces they need ("ports"), in each area's `Ports/` folder.
- **Infrastructure** implements the ports: Postgres, Redis, SMTP, the in-memory stores, background services.
- **Presentation** holds endpoints, the hub, the SignalR side of the ports (`IBroadcaster`, `IConnectionTerminator`) and the DI composition root.

## Alternatives considered

Keeping one project with layer folders was the starting point. The move to separate projects isn't explained in the commits; what it changes is that project references make the compiler enforce the direction, where folders relied on discipline.

## Consequences

- Domain and UseCases have no reference to EF Core, Redis, SignalR or ASP.NET; replacing an adapter doesn't touch business code.
- Use cases can be unit-tested with fakes of their ports ([0020](0020-hand-rolled-fakes-and-real-containers-for-tests.md)).
- More projects and more interfaces to navigate than a single-project app.
- UseCases still references MediatR ([0005](0005-cqrs-with-mediatr-bypassed-on-the-hot-path.md)); Domain doesn't.
