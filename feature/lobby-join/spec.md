# Connect To Lobby

## Summary

Players join a lobby through a SignalR hub method (not a REST endpoint). When a player clicks "Join" on a lobby, the client establishes a WebSocket connection and calls `ConnectToLobby`. The server validates the request, creates a `LobbyPlayer`, adds the connection to a SignalR group, and broadcasts the join to all players in the lobby. Private lobbies require a separate invite code validation step before connecting.

## Goals

- Players join lobbies via SignalR, not REST, since they need a persistent connection for real-time updates
- Server validates: lobby exists, not full, status is "waiting", player is authenticated
- Private lobbies require a valid invite code (validated via a separate hub method before connecting)
- A `LobbyPlayer` entity is created and added to the lobby when the player joins
- All players in the lobby are notified when someone joins
- The joining player receives the current lobby state (players list, game mode, host info)

## Non-Goals

- Matchmaking or auto-assignment to lobbies
- Reconnection logic (handled in a separate feature)
- Starting the race (separate feature)
- Chat or messaging within the lobby

## User Experience

1. Player sees a lobby list on the home page
2. Player clicks "Join" on a public lobby
3. Client connects to SignalR hub with the player's token
4. Client calls `ConnectToLobby(lobbyId)`
5. Server validates and adds the player
6. Player sees the lobby room with the current player list
7. Other players in the lobby see the new player appear

For private lobbies:
1. Player clicks "Join" on a private lobby
2. Client prompts for an invite code
3. Client calls `ValidateInviteCode(lobbyId, code)` first
4. If valid, client calls `ConnectToLobby(lobbyId)`
5. Same flow as public from here

## Edge Cases

- **Lobby is full (30 players)**: Return error to caller, do not add to group
- **Lobby doesn't exist**: Return error — lobby may have been removed between list fetch and join attempt
- **Lobby status is not "waiting"**: Race already started — reject with clear message
- **Player is already in the lobby**: Don't create a duplicate `LobbyPlayer` — could happen if client retries after a network glitch
- **Player is already in a different lobby**: Decide whether to allow multi-lobby or force leave first
- **Invalid/missing token**: Hub rejects — `Context.User` has no identity
- **Private lobby, no invite code validated**: `ConnectToLobby` should check if the lobby is private and reject if the player hasn't validated the code
- **Wrong invite code**: `ValidateInviteCode` returns error, client doesn't proceed
- **Player disconnects mid-join (between validation and group add)**: SignalR handles cleanup via `OnDisconnectedAsync`
- **Host disconnects after player joins**: Host promotion logic (separate concern, but the join flow should set up the data correctly for it)
- **Concurrent joins pushing past max**: `LobbyStore` uses `ConcurrentDictionary`, but player count check + add is not atomic — needs a lock or atomic check on the `Lobby` entity

## Open Questions

1. Should a player be allowed to be in multiple lobbies at once, or must they leave one before joining another? HE SHOULD NOT
2. How should invite code validation be tracked server-side — store a "validated" flag per connection, or use a time-limited token? time-limited token but let's add it later
3. Should the `DisplayName` be passed during `ConnectToLobby`, or should it come from the `User` entity (set at registration)? should come from user entity
4. What information should be broadcast to the group when a player joins — just ID and name, or also join order? name 
5. Should `ConnectToLobby` also handle the host joining their own lobby after creating it, or does `CreateLobby` already add the host as a `LobbyPlayer`? yes I guess it should because create lobby just assigns the hostId of the player so in connection it can be validated if the player is host so he doesn't require an invite code

## Notes

- The `JoinLobbyEndpoint` (REST) can be removed or repurposed — the hub handles joining now
- `LobbyPlayer` creation should happen inside the handler/service, not in the hub directly
- The hub should use `IHandler<JoinLobbyCommand, JoinLobbyResult>` to keep business logic out of the hub
- SignalR group name convention: `$"lobby-{lobbyId}"`
