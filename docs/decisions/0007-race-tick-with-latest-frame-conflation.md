# 0007. 200 ms race tick with latest-frame conflation

- **Status:** Accepted
- **Date:** 2026-05-18 (200 ms tick), 2026-07-02 (conflation)
- **Sources:** [f542493](https://github.com/ciq312/Faster-n-Faster/commit/f542493) (change race tick to 200 ms), [1593e55](https://github.com/ciq312/Faster-n-Faster/commit/1593e55) (decouple race tick via broadcaster), [611b8f9](https://github.com/ciq312/Faster-n-Faster/commit/611b8f9) (conflate race state frames), [fe7eff0](https://github.com/ciq312/Faster-n-Faster/commit/fe7eff0) and [8cb5157](https://github.com/ciq312/Faster-n-Faster/commit/8cb5157) (1 ms tick experiment, 200 ms restored), [2d9623d](https://github.com/ciq312/Faster-n-Faster/commit/2d9623d) (race snapshot as a record struct), [afcb400](https://github.com/ciq312/Faster-n-Faster/commit/afcb400), [#23](https://github.com/ciq312/Faster-n-Faster/pull/23) (deregister lobby tick)

## Context

Every player in a race needs to see every other player's cursor move. Two problems follow:

1. **Fan-out.** Broadcasting on every progress update means each of N players' updates goes to N players, so traffic grows with the square of the lobby size and with typing speed.
2. **Slow receivers.** Sending a frame to a lobby can take longer than the time until the next one, for example when one player's connection is slow. If frames queue up, that lobby falls further and further behind, receiving positions that are already out of date, and the queue grows without limit.

## Decision

Two mechanisms, one for each problem.

### The tick decouples broadcasting from typing

Progress updates only change in-memory state ([0001](0001-server-authoritative-race-progress.md)). `RaceTickService` broadcasts on its own clock:

```
every 200 ms:
  for each lobby in RaceTickRegistry (in parallel):
    countdown over?  → start the race, send RaceStarted
    racing?          → race.RetryPendingClaims()            (grant progress the budget held back)
                       snapshot = race.GetSnapshot()        (inside the race's gate)
                       players  = lobby.GetPlayerIds()      (inside the lobby's gate)
                       conflator.Publish(lobby, players, snapshot)
```

However fast people type, each lobby gets at most 5 frames a second.

### The conflator keeps only the latest frame

`RaceStateConflator` sits between the tick and the network. **What it's for:** the tick never waits for the network, and a lobby never receives stale frames. **Why:** for race positions only the newest frame matters, since a newer frame replaces everything in an older one. So when the network is slower than the tick, the right move is to drop the old frame, not queue it.

**How:** each lobby has one slot (`Latest`) and one flag (`Running`, meaning a send loop is active).

```
Publish(lobby, frame):                         ← called by the tick, never awaits
  Latest = frame                               (Interlocked.Exchange: overwrite any unsent frame)
  if Running 0 → 1 (CompareExchange):          (only one sender per lobby)
    start Pump in the background

Pump:
  do
    while (frame = take Latest, leave null) != null:
      await send frame to the lobby's players  (the slow part)
    Running = 0
  while Latest != null and Running 0 → 1      (re-check before exiting)
```

- If a send is still running when the next tick publishes, the new frame overwrites the slot. The pump picks up whatever is newest when it finishes, and the overwritten frame is never sent.
- At most one frame per lobby waits in memory, and at most one send per lobby is in flight.
- `Publish` only swaps a reference and maybe starts a task, so one slow lobby can't delay the tick for the others.
- The final re-check closes a gap: a frame published after the inner loop found the slot empty, but before `Running` was reset, saw `Running == 1` and didn't start a pump. Without the re-check it would wait for the next tick. With it, the exiting pump sees the frame and takes the flag back.
- No locks: the slot and flag are swapped with `Interlocked`, so the tick never blocks on a send.

Snapshot elements (`ParticipantSnapshot`) are a `record struct`, so building a frame doesn't allocate one object per player per tick.

## Alternatives considered

- **Broadcast on every update.** The N² fan-out above.
- **Queue every frame per lobby.** A slow lobby would receive a growing backlog of outdated positions, and memory would grow with it.
- **Await the send inside the tick.** One slow lobby would delay every other lobby's tick.

## Consequences

- Broadcast cost is flat in typing speed: one frame per lobby per tick at most.
- Other players' cursors move in 200 ms steps; the client interpolates (caret lerp) to hide it.
- Load testing showed the allocation rate spiking during races while snapshots were classes. Making them a struct cut it noticeably, and broadcast latency under load dropped by over 200 ms RTT.
- A lobby must be deregistered from the tick when its race ends (#23); otherwise the tick keeps broadcasting empty state. The conflator drops slots for lobbies that are no longer racing (`Prune`).
- Tests can hold a send "in flight" (`GatedBroadcaster`) and check the latest-frame behaviour without sleeping.
