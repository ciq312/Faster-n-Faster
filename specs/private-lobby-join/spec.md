# Private Lobby Join

## Summary

Private lobbies currently have an invite code generated in `LobbySettings`, but there is no way to actually use it. Players need a way to enter an invite code, resolve it to a lobby, and join — with the server enforcing that you **cannot** join a private lobby without the correct code, even if you know the lobby's URL/GUID.

## Goals

- Players can join a private lobby by entering its invite code on the lobbies page
- The invite code is the **only** way into a private lobby — knowing the lobbyId/URL is not enough
- The host sees the invite code after creating a private lobby so they can share it
- Server-side enforcement: `JoinLobbyHandler` rejects joins to private lobbies without a valid invite code
- Reconnects to a private lobby (pending removal cancellation) should still work without re-entering the code

## Non-Goals

- Invite code expiration or rotation (host can't regenerate codes yet)
- Rate-limiting on code lookups (brute-force protection)
- Persisting "authorized players" — every join attempt to a private lobby must include the code
- UI for the host to copy/share the code (clipboard button, etc.) — just display it for now

## User Experience

### Creating a private lobby (host)

1. Host creates a lobby with "private" visibility (existing flow)
2. `CreateLobbyHandler` generates a unique invite code and sets it on the lobby
3. The `CreateLobbyResult` now includes the `inviteCode`
4. Frontend receives the code and navigates to the lobby page as usual (the host is exempt from code validation since they created it)
5. The invite code is displayed somewhere visible in the lobby UI (e.g., in the topbar next to the lobby name) so the host can read it out or copy it

### Joining via invite code (other players)

1. Player enters the invite code in the "join via invite code" form on the lobbies page
2. Frontend calls `GET /api/lobbies/by-code/{code}` — returns `{ lobbyId }` or 404
3. On success, frontend navigates to `/lobby/{lobbyId}` and stores the invite code temporarily (e.g., in a ref or state)
4. `useLobby` calls `ConnectToLobby(lobbyId, inviteCode)` via SignalR — the hub passes the code through to `JoinLobbyHandler`
5. `JoinLobbyHandler` checks: if the lobby is private, the provided code must match `lobby.LobbySettings.InviteCode`. If it doesn't match (or is null), reject with an error
6. Player joins the lobby normally

### Joining via URL without code (blocked)

1. A viewer copies the streamer's URL `/lobby/{lobbyId}` and navigates to it
2. `useLobby` calls `ConnectToLobby(lobbyId)` with no invite code
3. `JoinLobbyHandler` sees the lobby is private, no code provided — rejects
4. Frontend receives the error and redirects back to the lobbies page with a message like "This lobby requires an invite code"

### Public lobbies (unchanged)

1. Public lobbies work exactly as they do today — no invite code needed
2. `JoinLobbyHandler` skips code validation when `IsPrivate == false`

## Edge Cases

- **Code collision during generation**: `GenerateUniqueInviteCode` already handles this with a retry loop — no change needed
- **Lobby not found by code**: Endpoint returns 404, frontend shows "Invalid invite code"
- **Lobby found but full**: Code resolves successfully, but `AddPlayer` throws "Lobby is full" — handled by existing error flow
- **Lobby found but racing**: Code resolves, but `AddPlayer` throws "Lobby is not accepting players" — existing error flow
- **Reconnect to private lobby**: Player was in the lobby, disconnected, has a pending removal. `JoinLobbyHandler` already short-circuits on `TryGetPendingRemoval` **before** the invite code check — reconnects should work without the code
- **Host navigating back to their own lobby**: Host is already a player, `IsPlayerInLobby` returns true — but the code check would still fire. The handler should skip code validation if the player is already in the lobby
- **Player already in lobby (e.g., page refresh)**: Same as above — skip code validation for existing players
- **Invite code casing**: Codes are uppercase alphanumeric. The lookup should be case-insensitive (normalize to uppercase) to avoid user confusion
- **Stale code in frontend**: If the player stores the invite code in state and the lobby is deleted, the `by-code` endpoint returns 404 — frontend handles it

## Resolved Questions

1. **Invite code display**: Always visible in the lobby topbar next to the lobby name.
2. **Hub method signature**: Second parameter on existing `ConnectToLobby(Guid lobbyId, string? inviteCode = null)`.
3. **LobbyStateDTO includes invite code**: Yes — all players can see it and invite others.
4. **Auth on by-code endpoint**: Yes, requires authentication (consistent with existing lobby endpoints).

## Notes

- `LobbySettings` already has `InviteCode`, `SetInviteCode()`, and `GenerateUniqueInviteCode()` — the entity layer is ready
- `CreateLobbyHandler` currently does **not** call `GenerateUniqueInviteCode` — this is the first thing to wire up
- The `ILobbyStore` needs a new method: `GetByInviteCode(string code)` — scans in-memory lobbies by code
- The `JoinLobbyCommand` needs a new optional field: `string? InviteCode`
- The `ConnectToLobby` hub method signature changes from `(Guid lobbyId)` to `(Guid lobbyId, string? inviteCode = null)`
- The `by-code` endpoint is a simple read — it doesn't join the player, just resolves code to lobbyId
