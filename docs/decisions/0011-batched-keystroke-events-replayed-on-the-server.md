# 0011. Batched keystroke events replayed on the server

- **Status:** Proposed
- **Date:** 2026-10-07

## Context

Progress snapshots ([0001](0001-server-authoritative-race-progress.md)) have limits that the anti-cheat changes can't fix:

- `mistakes` is reported by the client. The server takes `max(stored, reported, observed)`, but "observed" covers only wrong characters still visible in a snapshot, so a modified client can hide mistakes it corrected between sends.
- The typing rules exist twice, in `useTyping.jsx` and in `RaceParticipant`, and can drift apart.
- Every snapshot carries the whole `typed` string, up to about 270 characters.
- No per-keystroke timing reaches the server, so rhythm-based bot detection isn't possible.

## Decision

Replace `UpdateRaceState` with batched keystroke events that the server replays. This changes the hub contract, so backend and frontend have to ship together.

- A new hub method, such as `SubmitKeys(int firstSeq, KeyEvent[] events)`. Each event is a key (a character or Backspace) plus milliseconds since the previous key.
- **The message rate stays as it is.** The client buffers keystrokes and flushes every 100 ms; the throttle changes from latest-wins to accumulate. Nothing is dropped, only delayed.
- A domain typing engine replays each batch through the same rules the client applies and derives typed, index and mistakes itself. The `mistakes` parameter goes away.
- Batches carry sequence numbers and must be contiguous. A gap or duplicate is rejected, and the server sends its `{seq, state}` so the client can resync. This also replaces the `(-1, "")` reload signal.
- The number of events per batch is capped.
- Kept as they are: `ProgressBudget` (applied to replayed growth), the `RaceState` tick broadcast, and the sanction ladder (a rejected batch raises `RaceViolationEvent`).
- The JavaScript and C# rule implementations are kept in sync by shared test vectors (JSON: key stream → expected state) run by both test suites. This depends on frontend tests existing first.
- Alongside it, `RaceState` gets slimmer (no `typed` or `mistakes` for other players), and a personal `RaceProgress` event lets a client resync its own state.

## Alternatives considered

Keep snapshots and add more checks. Each check only covers what a snapshot shows, which is the limit described above.

## Consequences

- Accuracy becomes exact for honest clients, and the typing rules are enforced by construction.
- A modified client can still send a clean event stream; unforgeable accuracy is out of scope. Rhythm or statistics-based detection becomes possible later, as separate work.
- The server does more work per batch than per snapshot.
- Acceptance bar: no more hub calls per player than today (about 10 a second while typing, none while idle), a typo plus backspace inside one flush window matches between client and server, and a reload mid-race resumes from the server's sequence without a violation.
