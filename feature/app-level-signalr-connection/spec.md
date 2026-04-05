# App-Level SignalR Connection

## Summary

Move the SignalR connection from being created and destroyed on every Lobby page visit to a single persistent connection created once after authentication and kept alive across all page navigations. This eliminates unnecessary disconnects when players browse the leaderboard or lobby list while in a lobby, and enables the Navbar to show a "Return to Lobby" link. The backend requires zero changes — the frontend is the only layer affected.

## Goals

- Create the SignalR connection once after authentication, persist it for the entire session
- Navigating between pages does not drop the WebSocket connection
- Lobby page calls `ConnectToLobby` on mount and `LeaveLobby` on unmount instead of `connection.stop()`
- Navbar is lobby-aware: shows a "Return to Lobby" link when the player is in an active lobby
- Event handlers are properly cleaned up when leaving/switching lobbies
- Auto-reconnect re-joins the active lobby automatically
- Race state (countdown, passage, opponents, results) resets cleanly when leaving a lobby

## Non-Goals

- No backend changes — the hub already supports `ConnectToLobby` / `LeaveLobby` as explicit methods
- REST endpoints for lobby list, lobby creation, leaderboard, and auth stay as REST — no migration to SignalR hub methods
- No changes to the fast reconnect grace period (stays at 15 seconds) — with app-level connection, it only fires on real network drops or tab close, which is the intended use case
- No multi-lobby support — a player can only be in one lobby at a time

## Design Decisions

- **REST stays for request-response calls.** Lobby browsing (`GET /api/lobbies`), lobby creation (`POST /api/lobbies`), leaderboard (`POST /api/leaderboards`), and auth endpoints remain as HTTP. The WebSocket adds no traffic benefit for one-off queries — the payload is identical either way, and HTTP connections are already reused via keep-alive.
- **Connection starts after authentication.** The Registration page has no need for a WebSocket. Once the player has a token, the connection is established and persists until logout or tab close.
- **Grace period unchanged.** 15 seconds is appropriate for real network interruptions, which are now the only scenario that triggers `OnDisconnectedAsync`.

## User Experience

### Happy path
1. Player opens the app, lands on Registration page — no WebSocket connection yet
2. Player logs in or registers as guest — connection is established
3. Player browses lobby list (REST fetch), creates or joins a lobby — `ConnectToLobby` is called
4. Player clicks "Leaderboard" in the Navbar — Navbar shows a "Return to Lobby" link, connection stays alive, player remains in the lobby
5. Player clicks "Return to Lobby" — navigates back to the lobby page, lobby state is re-synced via `LobbyState` event
6. Player clicks "Leave Lobby" — `LeaveLobby` is called, Navbar link disappears, player is back to browsing

### Navigation away during a race
1. Player is mid-race and clicks Leaderboard in Navbar
2. Connection stays alive — race state continues on the server
3. Navbar shows "Return to Lobby" link
4. Player navigates back — lobby page re-subscribes to events, receives current race state

### Tab close / network drop
1. Connection drops — backend `OnDisconnectedAsync` fires
2. 15-second fast reconnect grace period starts
3. If player reopens the app and re-authenticates within 15 seconds, `ConnectToLobby` cancels the pending removal
4. If not, player is removed from the lobby as before

## Architecture

### New: SignalR Context Provider
- Wraps the app at the top level (below Router, above routes)
- Creates and owns the single SignalR connection
- Exposes: connection instance, connection state, active lobby ID
- Starts the connection after a valid token exists in localStorage
- Only calls `connection.stop()` on logout or unmount of the entire app

### Modified: useLobbyConnection hook
- No longer creates or stops the connection — consumes it from context
- Registers event handlers on mount, unregisters them (`connection.off`) on unmount
- Calls `ConnectToLobby(lobbyId)` on mount
- Calls `LeaveLobby()` on unmount (not `connection.stop()`)
- Sets active lobby ID in context so Navbar can read it

### Modified: Navbar
- Reads active lobby ID from context
- When set, renders a "Return to Lobby" link pointing to `/lobby/{lobbyId}`

### Modified: App.js
- Wraps routes with the SignalR context provider

### Unchanged
- All backend code (GameHub, handlers, LobbyService, etc.)
- REST hooks (useFetchLobbies, useCreateLobby, useFetchLeaderboard, auth hooks)
- Game components (TypingArea, LobbyPlayerCard, RaceResults)
- Game hooks (useTyping, useCharPositions)

## Edge Cases

- **Player navigates directly to a lobby URL while unauthenticated**: connection hasn't started yet. Must wait for auth before connecting. Redirect to Registration or show an error.
- **Player refreshes the page mid-lobby**: connection is re-created from scratch (new connectionId). The fast reconnect grace period handles this — old connection triggers `OnDisconnectedAsync`, new connection calls `ConnectToLobby` which cancels the pending removal.
- **Player opens multiple tabs**: each tab has its own connection and connectionId. Backend tracks them independently. Second tab joining the same lobby could cause duplicate player issues — current `IsPlayerInLobby` check handles this, but the first tab's connection would be stale.
- **Token expires while connection is alive**: SignalR uses the token only during negotiation. An expired token won't affect an existing connection, but auto-reconnect after a drop would fail. Need to handle reconnect failure by redirecting to login.
- **Auto-reconnect fires while in a lobby**: must re-call `ConnectToLobby` after reconnect succeeds to rejoin the SignalR group (groups are not persisted across reconnects by default). Use `connection.onreconnected` callback.
- **Player is kicked while on another page**: `Kicked` event fires but Lobby page isn't mounted to handle it. The provider should listen for `Kicked` and clear the active lobby ID so the Navbar link disappears.
- **Race ends while player is on another page**: `RaceEnded` event fires but no handler is registered. When player returns, lobby page mounts and receives current state via `LobbyState`. Old race results may be lost — acceptable for v1, or the provider could cache the last `RaceEnded` payload.
- **Event handler double-registration**: if Lobby page unmounts and remounts without cleanup, handlers stack. Must call `connection.off("LobbyState")` before `connection.on("LobbyState", handler)`.

## Open Questions

1. Should the Navbar "Return to Lobby" link be a button that navigates, or should it also show lobby info (name, player count)? button that navigates
2. When a player is kicked while on another page, should they see a toast/notification, or just silently lose the Navbar link? error banner that already exists
3. If a race ends while the player is on another page, should the provider cache results so they're available when the player returns?
probably yes
4. Should the provider also handle the `LobbyState` event at app level (to keep Navbar info fresh), or only the Lobby page?
5. What happens when the player clicks "Return to Lobby" but the lobby was deleted/closed while they were away? Need an error state and cleanup.
show that error can't find lobby and navigate to lobbies

## Notes

- `withAutomaticReconnect()` reconnects the WebSocket but does NOT rejoin SignalR groups. The `onreconnected` callback must re-invoke `ConnectToLobby` for the active lobby.
- The `connectionId` changes after auto-reconnect. The backend's `TrackConnection` handles this naturally since `ConnectToLobby` calls it with the new ID.
- React strict mode in development double-mounts components. Event handler registration/cleanup must be idempotent to avoid issues during development.
