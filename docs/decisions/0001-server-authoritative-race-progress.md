# 0001. Server-authoritative race progress

- **Status:** Accepted
- **Date:** 2026-05-10, tightened 2026-10-07
- **Sources:** [80dc6eb](https://github.com/ciq312/Faster-n-Faster/commit/80dc6eb) (rework WPM validation), [01580f7](https://github.com/ciq312/Faster-n-Faster/commit/01580f7) (keystroke throttling), [#18](https://github.com/ciq312/Faster-n-Faster/pull/18) (typing budget, server-side mistake floor), [#21](https://github.com/ciq312/Faster-n-Faster/pull/21) (WPM computing)

## Context

Races are competitive and results go on a public leaderboard, so a modified client must not be able to report a faster or cleaner run than it typed. At the same time, typing has to feel instant: waiting for a server round-trip before showing each character isn't acceptable.

## Decision

The client renders typing locally and reports progress; the server decides what counts.

- The client sends a snapshot `(index, mistakes, typed)` at most every 100 ms through `GameHub.UpdateRaceState`. Intermediate snapshots are dropped (latest wins).
- `RaceParticipant.UpdateProgress` rejects a snapshot whose typed prefix doesn't match the passage, whose overflow past the last correct character exceeds 6 (`MaxOverflow`), or that has a space after a wrong character. A rejection raises `RaceViolationEvent`, which feeds the sanction ladder: race withdrawal, then strikes, then a 24 h suspension for registered players.
- New characters are granted from a token bucket, `ProgressBudget` (`AntiCheat:MaxWpm` 350 at 5 characters per word, plus `BudgetSlack` 6). Progress beyond the budget isn't rejected: it's kept as a pending claim and granted on later ticks as the budget refills.
- Mistakes are `max(stored, reported, observed)`: the count can't go down, and wrong characters the server can see in the snapshot count even if the client didn't report them.
- WPM and accuracy are computed on the server from the granted progress and the server's clock.
- `(-1, "")` is accepted as a "the client just reloaded" signal, so a reload mid-race doesn't look like progress going backwards.

## Alternatives considered

- **Trust the client's numbers.** Rejected: any edited request would set a leaderboard record.
- **Replay every keystroke on the server.** Gives exact accuracy and per-key timing, but changes the hub contract on both sides. Proposed as the next step in [0011](0011-batched-keystroke-events-replayed-on-the-server.md).

## Consequences

- Honest players see no latency; cheating is limited to what passes the rules and the budget.
- The typing rules exist twice, in `useTyping.jsx` and in `RaceParticipant`, and can drift apart.
- A modified client can still hide mistakes it corrected between two snapshots, because the server only sees what's left in each snapshot.
- Every snapshot carries the whole `typed` string from the start of the passage.
