# Tick-Based Server Throttle Race

## Summary

During a race, a background hosted service runs a `PeriodicTimer` at 50ms intervals (20 ticks/sec). Clients send their progress snapshots on every keystroke — no client-side throttling. The server validates each snapshot via `RaceParticipant.ValidateUpdate()` as it arrives, but does **not** broadcast immediately. Instead, the background service collects the latest validated state of all connected players and broadcasts a single `RaceState` message per tick. Disconnected players are excluded from ticks and results. Finish events (`PlayerFinished`, `RaceEnded`) fire as separate immediate events, not bundled in ticks. This is independent from `LobbyStateBroadcaster`, which remains responsible for lobby-level events (join, kick, host transfer, config).

## Goals

- Decouple client input frequency from server broadcast frequency — clients send on every change, server broadcasts at a fixed 50ms tick.
- Reduce broadcast volume: one message per tick per lobby instead of one per player update.
- Include race metadata in each tick: all player positions, WPM, and time remaining (timer mode).
- Keep local typing latency at zero — the client never waits for a tick to render its own keystrokes.
- Preserve existing anti-cheat validation in `RaceParticipant.ValidateUpdate()`.
- Enable timer mode: server-controlled countdown with auto-finish when time expires.
- Keep `LobbyStateBroadcaster` responsible for lobby events only. Race ticks are a separate concern.

## Non-Goals

- Changing how `TypingArea` renders typed characters on the client.
- Implementing net WPM or accuracy changes.
- Adding Redis pub-sub or horizontal scaling.
- Persisting race results to the database.
- Changing lobby management (join, kick, host transfer).
- Client-side throttling of snapshots.

## User Experience

1. Host configures race and clicks "start race."
2. All players see a 3-second countdown (unchanged).
3. Race starts — each player types locally. Keystrokes render instantly.
4. Client sends progress snapshots to the server via `UpdateLobbyState(index, totalTyped, mistakes)` on every change.
5. Every 50ms the background service broadcasts `RaceState` to the lobby group containing all connected players' positions, WPM, and race metadata.
6. Each client receives the tick and updates the progress / WPM of **other** players. The client **ignores its own position** from the tick and uses local state.
7. When a player finishes, a separate `PlayerFinished` event fires immediately (not bundled in the tick).
8. When all connected players finish (or timer expires), a separate `RaceEnded` event fires immediately with final results.
9. For timer mode, each tick includes `timeRemaining`. When it hits zero the server fires `RaceEnded`.
10. Disconnected players are excluded from tick broadcasts and race results.

## Edge Cases

- **Player sends no updates during a tick interval**: their last known position is broadcast. Other clients see them frozen.
- **Player disconnects mid-race**: excluded from subsequent ticks and from final results.
- **All players disconnect**: the tick loop detects an empty lobby and stops to avoid leaking background tasks.
- **Timer expires while a player is mid-keystroke**: server fires `RaceEnded` on the tick that crosses the deadline. The player's last validated index is their final position.
- **Tick fires but race hasn't started or already finished**: the background service only ticks lobbies with status `racing`. Starts when a lobby transitions to `racing`, stops on `finished`.
- **Very fast typist sends many snapshots between ticks**: all validated and applied sequentially via `ProcessUpdate()`. The tick broadcasts the latest validated state.
- **Solo race (one player)**: tick loop runs normally. Timer countdown and finish detection still work.
- **Multiple lobbies racing simultaneously**: the background service iterates all racing lobbies each tick. Each lobby gets its own broadcast.

## Open Questions

1. **Tick lifecycle**: how does the background hosted service know when a lobby starts/stops racing? Options: (a) the service polls `ILobbyStore` for racing lobbies each tick, (b) `GameHub.StartRace` registers the lobby with the service, and the service deregisters on finish.
2. **Thread safety**: `Race.ProcessUpdate()` is called from SignalR hub threads (per-player), while the tick service reads participant state from a background thread. Does the current lock on `Lobby` cover this, or does `Race` / `RaceParticipant` need its own synchronization?

## Notes

- Current `UpdateProgress` broadcasts `PlayersProgress` on every call — this is the main thing being replaced. Renamed to `UpdateLobbyState`. Validation logic in `Race.ProcessUpdate()` and `RaceParticipant.ValidateUpdate()` stays intact.
- `LobbyStateBroadcaster` remains responsible for lobby-level events (join, kick, host transfer, config changes). Race tick broadcasting is a separate mechanism.
- `TimerRace` already has `TimerDurationSeconds` but no server-side countdown. The tick loop naturally adds it.
- WPM is included in each tick so every player can see others' live speed.
- Tick interval: 50ms (20 ticks/sec). Max 30 players per lobby — each tick message contains ~30 small player objects, well within SignalR limits.
- `PlayerFinished` and `RaceEnded` are separate immediate events, not bundled in ticks, to avoid up to 50ms delay on finish announcements.
