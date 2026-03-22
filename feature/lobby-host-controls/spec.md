# Lobby Host Controls

## Summary

The lobby host has exclusive control over lobby management actions: transferring host to another player, starting the race, configuring race parameters (game mode, word count, timer duration), and kicking players. All actions are performed via SignalR hub methods since the lobby is real-time. Non-host players attempting these actions are rejected. Automatic host promotion on disconnect is already implemented.

## Goals

- Only the host can perform management actions — all hub methods validate `lobby.HostPlayerId == userId` before proceeding
- Host can transfer host role to another connected player in the lobby
- Host can start the race (transition lobby status from "waiting" to "racing")
- Host can change race configuration while the lobby is in "waiting" status (switch game mode, adjust word count or timer duration)
- Host can kick a player from the lobby
- All actions broadcast updates to the lobby group so every player's UI stays in sync

## Non-Goals

- Automatic host promotion on disconnect (already implemented in `OnDisconnectedAsync`)
- Private lobby invite code management (deferred)
- Race gameplay logic (typing, scoring, finishing) — separate feature
- Vote-kick or democratic host transfer — host has unilateral control

## User Experience

**Transfer Host:**
1. Host clicks a player in the player list and selects "Make Host"
2. Server validates and transfers host role
3. All players see the host indicator move to the new player
4. The old host loses management controls, the new host gains them

**Start Race:**
1. Host clicks "Start Race" button
2. Server validates lobby has at least 1 connected player and a race is configured
3. Lobby status transitions from "waiting" to "racing"
4. All players receive a "RaceStarted" event

**Configure Race:**
1. Host changes game mode dropdown or adjusts word count / timer duration
2. Server updates the lobby's race configuration
3. All players see the updated settings in real time

**Kick Player:**
1. Host clicks a player in the player list and selects "Kick"
2. Server removes the player from the lobby and disconnects them from the SignalR group
3. Kicked player receives a "Kicked" event and is removed from the lobby
4. Other players see the player disappear from the list

## Edge Cases

- **Non-host tries a host action**: Reject with error, do not broadcast anything
- **Host tries to kick themselves**: Reject — use "leave lobby" instead
- **Host tries to transfer to a disconnected player**: Reject — new host must be connected
- **Host tries to transfer to themselves**: Reject (no-op)
- **Host starts race with no race configured**: Reject — must have word count or timer set
- **Host changes configuration while race is in progress**: Reject — config changes only allowed in "waiting" status
- **Host kicks a player who is mid-disconnect**: Player may already be gone — handle gracefully (no error if player not found)
- **Host transfers host, then old host tries to start race**: Rejected — they're no longer host
- **Last player gets kicked**: Lobby becomes empty — decide whether to clean it up or leave it
- **Kicked player tries to rejoin**: Should they be allowed to? No ban list in v1, so yes

## Open Questions

1. Should there be a countdown before the race starts (e.g., 3-2-1), or does it start immediately when the host clicks "Start Race"?
2. When the host reconfigures the race (e.g., switches from wordcount to timer), should the old race config be cleared immediately or require confirmation?
3. Should solo play (host is the only player) be allowed to start a race?
4. When a player is kicked, should the server forcefully close their SignalR connection, or just remove them from the lobby/group and let the client handle navigation?
5. Should there be a limit on how many times the host can transfer host role (to prevent trolling)?

## Notes

- All hub methods follow the same pattern: extract userId from `Context.User`, validate host, delegate to handler, broadcast result
- Race configuration changes reuse `Lobby.ConfigureWordRace` and `Lobby.ConfigureTimerRace` which already exist
- `Lobby.TransitionStatus("racing")` already enforces the waiting → racing transition
- Kicking a player is similar to disconnect cleanup but initiated by the host rather than a lost connection
- The host validation check (`lobby.HostPlayerId == userId`) should probably be extracted into a helper to avoid duplication across hub methods
