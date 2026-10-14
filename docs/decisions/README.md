# Architecture decisions

One file per decision: what problem it answered, what was chosen, what else was on the table, and what it costs. For how the pieces fit together, start with [`../architecture.md`](../architecture.md).

Records 0001–0012 were written on 2026-10-14 from the commit history, pull requests and the code. Where the reasoning wasn't written down at the time, the record says so instead of guessing.

## Index

### Foundations

| # | Decision | Status | Date |
|---|---|---|---|
| [0001](0001-server-authoritative-race-progress.md) | Server-authoritative race progress | Accepted | 2026-05-10 |
| [0002](0002-live-game-state-in-memory-on-one-instance.md) | Live game state in memory on one API instance | Accepted | 2026-03-17 |
| [0003](0003-clean-architecture-over-layered.md) | Clean Architecture instead of a traditional layered architecture | Accepted | 2026-06-27 |
| [0004](0004-cqrs-with-mediatr-bypassed-on-the-hot-path.md) | CQRS with MediatR, bypassed on the keystroke hot path | Accepted | 2026-03-17 |
| [0005](0005-domain-events-dispatched-after-the-gate.md) | Domain events as MediatR notifications, dispatched after the gate | Accepted | 2026-06-27 |

### Live game state and real-time

| # | Decision | Status | Date |
|---|---|---|---|
| [0006](0006-stores-as-ports-with-one-gate-per-aggregate.md) | Stores as ports, one async gate per aggregate | Accepted | 2026-07-02 |
| [0007](0007-race-tick-with-latest-frame-conflation.md) | 200 ms race tick with latest-frame conflation | Accepted | 2026-05-18 |
| [0008](0008-one-lobbystate-broadcast-per-changed-lobby.md) | One `LobbyState` broadcast per changed lobby | Accepted | 2026-09-20 |
| [0009](0009-broadcast-to-tracked-connections-not-groups.md) | Broadcast to tracked connections, not SignalR groups | Accepted | 2026-07-01 |
| [0013](0013-lobby-membership-follows-the-active-connection.md) | Lobby membership follows the active connection | Accepted | 2026-10-14 |

### Cross-cutting

| # | Decision | Status | Date |
|---|---|---|---|
| [0010](0010-http-status-codes-mapped-in-presentation.md) | Business errors as exception categories, mapped to HTTP in Presentation | Accepted | 2026-10-13 |

### Planned

| # | Decision | Status | Date |
|---|---|---|---|
| [0011](0011-batched-keystroke-events-replayed-on-the-server.md) | Batched keystroke events replayed on the server | Proposed | 2026-10-07 |
| [0012](0012-frontend-on-typescript-redux-toolkit-rtk-query.md) | Frontend on TypeScript, Redux Toolkit and RTK Query | Accepted, not started | 2026-10-13 |

## Statuses

- **Proposed:** agreed as the direction, not yet built.
- **Accepted:** in effect. "Accepted, not started" means the decision is made but no code exists yet.
- **Superseded by NNNN:** replaced; kept for history.

## Adding a record

Copy the template below to `NNNN-short-title.md` with the next number, add it to the index, and link it from [`../architecture.md`](../architecture.md) if it changes how the system fits together. Don't rewrite an accepted record when the decision changes: write a new one and mark the old one superseded.

```markdown
# NNNN. Title

- **Status:** Proposed | Accepted | Superseded by [NNNN](NNNN-....md)
- **Date:** YYYY-MM-DD
- **Sources:** PRs and commits

## Context

The problem and the forces at play.

## Decision

What was chosen, stated so someone can check the code against it. Name the principle or pattern it applies (open-closed, dependency inversion, decorator, …) and what that buys here.

## How it works

The mechanism: the flow, the loop or a short code sketch, with the classes involved. Skip it when the decision has no moving parts.

## Alternatives considered

What else was on the table and why it lost. "Not recorded" if nobody wrote it down.

## Consequences

What gets easier, what gets harder, what to watch.
```
