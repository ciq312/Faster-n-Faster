# 0013. Lobby membership follows the active connection

- **Status:** Accepted
- **Date:** 2026-10-14
- **Sources:** [#28](https://github.com/ciq312/Faster-n-Faster/pull/28) (session entry guarded by `ClearSessionIfActive`), [#44](https://github.com/ciq312/Faster-n-Faster/pull/44) (keep players with a live connection)

## Context

A player who loses their connection gets a 15 s grace period before `DisconnectCommand` removes them from the lobby. Two failures showed up:

- A stale disconnect, detected late (a dead socket times out well after the player is back), removed a player who already had a new connection.
- Pressing F5 on a page other than the lobby removed the player, although navigating inside the app never does. One SignalR connection spans `/lobbies`, `/lobby/:id`, `/profile/me` and `/leaderboard`, but only the lobby page's `RefreshLobby` cancelled a pending removal.

## Decision

A player stays in their lobby as long as they have a live hub connection anywhere in the app, and is removed 15 s after the last one goes away, or when they leave or are kicked. Whether they have one is read from the session map from [0009](0009-broadcast-to-tracked-connections-not-groups.md), which holds each user's current connection. This applies single source of truth (DRY): no second presence list that has to be kept in step with the sessions.

## How it works

```
OnConnectedAsync:
    StoreSession(userId, connectionId);       // SetUserSession
    Send(new ReconnectCommand(userId));       // cancels a pending removal

OnDisconnectedAsync:
    ClearSessionIfActive(userId, connectionId);
    Send(new FastReconnectCommand(...));      // waits 15 s, throws if cancelled
    if (GetActiveSession(userId) is null)
        Send(new DisconnectCommand(userId));
```

- **Session first, then cancel.** If the cancel came first, the timer of the old connection could end between the two lines, see no active session and remove the player.
- **One check at the end is enough.** Whatever order connects and disconnects arrived in before the check, removal depends only on whether a session is active at that moment. A connection that arrives between the check and the removal still sees the player removed, the same as one arriving just after the grace period.
- **The cancel on connect only ends the wait early** for a removal that was already pending when the connection arrived. A removal that starts after it, such as the old tab's disconnect after `AnotherSessionStarted`, runs its full 15 s, and the end check keeps the player.

## Alternatives considered

- **A lobby-presence owner claimed by `ConnectToLobby` and `RefreshLobby`.** A second registry that has to be cleared on every removal, and it still drops players on F5 outside the lobby page.
- **The connection list in `SignalRConnectionTerminator`.** It tracks every live connection, but drops one only after the hub's `OnDisconnectedAsync` returns, so a disconnecting connection would count itself during its own grace period. It also sits behind a terminate-only port.
- **Checking for another session only at disconnect time.** Misses a connection that arrives during the grace period.
- **An extra early check at disconnect time.** It would mostly skip a timer that the end check stops anyway. It would also close a microsecond-wide race where a leftover timer removes the player the moment their last connection closes, instead of 15 s later, which isn't worth a second branch.

## Consequences

- F5 or a network blip on any page keeps the player's place.
- A player, the host included, can hold a lobby slot from another page indefinitely. Others can't start a race while the host is away, as already happens with in-app navigation.
- Relies on one connection per user (0009): the frontend closes the older tab's connection on `AnotherSessionStarted`. A superseded tab that kept its socket open wouldn't count.
- After `RevokeAllSessions` (logout, password reset, account switch) a socket that is still open no longer counts as the user's connection, so the next disconnect removes the player after the grace period.
