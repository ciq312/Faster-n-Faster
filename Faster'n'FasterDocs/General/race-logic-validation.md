# Race Logic & Validation

## Design Principle: State Sync

The system uses **state sync**, not event streaming. The client does NOT send every keystroke to the server. Instead:

- Client handles all typing logic locally (instant feedback)
- Client sends **state snapshots** to the server at a throttled interval (~50-100ms)
- Server **validates** the snapshot (anti-cheat), stores the authoritative state, and broadcasts minimal progress to other players

This matches the TypeRacer architecture and scales much better than per-keystroke validation.

---

## Data Models

### Client (local state)

```
LocalState {
  index: number          // current correct position in passage
  totalTyped: number     // total keystrokes (correct + incorrect)
  mistakes: number       // incorrect keystrokes
  startedAt: number      // timestamp when race started
}
```

### Server (per-player authoritative state)

```
PlayerServerState {
  playerId: string

  index: number          // validated position
  totalTyped: number
  mistakes: number

  startedAt: number
  lastUpdateAt: number   // timestamp of last accepted update

  isFinished: boolean
  finishPosition: number?
  finishedAt: number?
}
```

---

## Client -> Server Messages

### 1. State Update (primary, throttled every 50-100ms)

```json
{
  "type": "update",
  "index": 42,
  "totalTyped": 50,
  "mistakes": 8
}
```

Sent on a fixed interval, NOT on every keystroke. Client-side `setInterval` at 50-100ms. If nothing changed since last send, skip.

### 2. Start Race

Handled by existing `StartRace()` hub method (host only).

### 3. Finish

**The client does NOT send a finish event.** The server detects finish when `index >= passage.length`. This prevents a cheat vector where a client claims to be finished without actually typing.

---

## Server -> Clients Messages

### 1. Players Progress (broadcast to lobby group)

```json
{
  "type": "PlayersProgress",
  "players": [
    { "playerId": "p1", "index": 42 },
    { "playerId": "p2", "index": 30 }
  ]
}
```

Minimal payload — only `playerId` + `index`. No WPM, no mistakes, no UI data. Other players only need to know cursor positions.

The server broadcasts this periodically (every 50-100ms) or whenever it processes an update. All players' positions are sent together in one message to reduce message count.

### 2. Player Finished

```json
{
  "type": "PlayerFinished",
  "playerId": "p1",
  "finishPosition": 1,
  "wpm": 85.2,
  "accuracy": 96.0
}
```

Broadcast when server detects a player reached the end of the passage.

### 3. Race Ended

```json
{
  "type": "RaceEnded",
  "results": [
    { "playerId": "p1", "finishPosition": 1, "wpm": 85, "accuracy": 96, "mistakes": 5 },
    { "playerId": "p2", "finishPosition": 2, "wpm": 72, "accuracy": 91, "mistakes": 12 }
  ]
}
```

Broadcast when all players finish (word race) or timer expires (timer race).

---

## Client Logic

### Keystroke Handling

```
onKeyPress(key):
  local.totalTyped++

  if key == passage[local.index]:
    local.index++
  else:
    local.mistakes++

  updateLocalUI()  // instant, no server round-trip
```

- Green/red highlighting is purely local
- Cursor movement is instant
- No waiting for server response

### Sending Updates (throttled)

```
setInterval(() => {
  if stateChangedSinceLastSend:
    send({ type: "update", index, totalTyped, mistakes })
}, 50)
```

### Rendering Other Players

Client receives `PlayersProgress` with `playerId + index` for each player. Render a cursor at `passage[index]` for each. No character-level data needed — just the position.

For smooth visuals, client can **interpolate** cursor movement between updates (lerp from old index to new index over the update interval).

---

## Server Validation (Anti-Cheat)

Every incoming `update` is validated before being accepted. Invalid updates are **clamped** (adjusted to valid bounds) rather than rejected outright, so network jitter doesn't kick legitimate players.

### 1. Index Jump Check

```
if (newIndex - oldIndex) > MAX_JUMP_PER_UPDATE:
  clamp newIndex to oldIndex + MAX_JUMP_PER_UPDATE
```

`MAX_JUMP_PER_UPDATE` = reasonable max chars typed in one update interval. At 200 WPM (world record pace), that's ~17 chars/sec, so ~1-2 chars per 50ms interval. Set to something like 5 to allow for burst typing with margin.

### 2. Speed Check

```
deltaTime = now - lastUpdateAt
charsPerSecond = (newIndex - oldIndex) / deltaTime

if charsPerSecond > MAX_CHARS_PER_SECOND:
  clamp to MAX_CHARS_PER_SECOND * deltaTime
```

### 3. Consistency Checks

```
if totalTyped < index:         // can't have typed fewer keys than correct chars
  reject

if mistakes > totalTyped:      // can't have more mistakes than total keystrokes
  reject

if mistakes < 0 || index < 0:  // sanity
  reject

if newIndex < oldIndex:        // index can't go backward
  clamp newIndex to oldIndex
```

### 4. Finish Detection (server-side only)

```
if player.index >= passage.length:
  player.isFinished = true
  player.finishPosition = nextPosition++
  player.finishedAt = now
  broadcast PlayerFinished
```

The client never tells the server it finished. The server decides based on validated index.

---

## Server Calculations

### WPM (calculated server-side for official results)

```
timeMinutes = (now - startedAt) / 60000
wpm = (index / 5) / timeMinutes
```

Standard: 5 characters = 1 word.

### Accuracy

```
accuracy = (index / totalTyped) * 100
```

Where `index` = correct characters, `totalTyped` = all keystrokes.

---

## What the Server Broadcasts

The server sends **minimal data**:

| To whom | What | When |
|---------|------|------|
| All in lobby | `PlayersProgress` (playerId + index only) | Periodically / on update |
| All in lobby | `PlayerFinished` (playerId, position, wpm, accuracy) | When player reaches end |
| All in lobby | `RaceEnded` (full results) | When race is over |
| Caller only | Rejection/clamp notification (optional) | When anti-cheat triggers |

---

## Why This Works

- **Lost packet?** Next snapshot corrects everything. No state desync.
- **Scalable.** ~10-20 updates/sec per player instead of ~10+ keystrokes/sec. Broadcast is one message with all positions, not N messages.
- **Anti-cheat.** Server validates physical limits. Can't jump ahead faster than humanly possible.
- **Instant UI.** Client doesn't wait for server. Typing feels local.
- **Simple cursors.** Other players' cursors are just an index into the same text. No character sync needed.

## Known Limitations

- Cannot detect "realistic speed" cheats (bots typing at human-like speeds). Acceptable for a typing game.
- Client could misreport mistakes (fewer than actual). Server can't verify individual characters. This only affects personal stats, not race position (which is based on index).
- No reconciliation if server clamps. The client may show a different index than the server accepts. For v1 this is fine — the visual difference is tiny (1-2 chars at most).

## Impact on Current Implementation

This replaces the current per-keystroke `SubmitKeystroke` approach. Instead of validating each character server-side, the server validates aggregate progress snapshots. The `Race.ProcessKeystroke()` method and character-level validation on the server will be replaced with snapshot validation logic.

The `PlayerProgress` broadcast changes from per-keystroke single-player updates to periodic all-players batch updates (`PlayersProgress`).
