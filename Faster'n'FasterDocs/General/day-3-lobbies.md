# Day 3 Plan — Apply Migration, Verify Stack, Build Lobby System

## Context

Day 2 wired up the infrastructure: EF Core + PostgreSQL, SignalR (in-memory for v1), FastEndpoints, and Docker orchestration. Migration is generated but not yet applied. Docker compose hasn't been verified end-to-end. Day 3 finishes the loose ends and builds the first real feature: the lobby system.

---

## Part 1: Finish Day 2 Loose Ends

### Step 1: Apply migration to PostgreSQL

```bash
docker compose up postgres -d
dotnet ef database update --project fasternfaster.api
```

Verify tables exist: `lobbies`, `lobby_players`, `race_results`, `comment_thresholds`.

---

### Step 2: Verify full Docker stack

```bash
docker compose up --build
```

Confirm:
- [ ] API starts without errors
- [ ] Health endpoint responds at `http://localhost:8080/api/health`
- [ ] Swagger loads at `http://localhost:8080/swagger`
- [ ] Frontend serves at `http://localhost:3000`
- [ ] SignalR ping/pong works from React app

---

## Part 2: Lobby System

### Step 3: Lobby service interface + implementation

**Create:** `fasternfaster.api/Lobby/ILobbyService.cs`
**Create:** `fasternfaster.api/Lobby/LobbyService.cs`

Responsibilities:
- `CreateLobbyAsync(gameMode, isPrivate)` — creates lobby, generates invite code if private (6-8 char alphanumeric, retry on collision)
- `GetLobbyAsync(lobbyId)` — fetch by ID with players
- `ListOpenLobbiesAsync()` — public lobbies in "waiting" status
- `JoinLobbyAsync(lobbyId, displayName, connectionId)` — validates max 30 players, creates LobbyPlayer
- `JoinByInviteCodeAsync(inviteCode, displayName, connectionId)` — find private lobby by code, then join
- `LeaveLobbyAsync(lobbyId, playerId)` — remove player, promote host if needed
- `StartRaceAsync(lobbyId, requestingPlayerId)` — only host can start, transitions waiting → racing

Register in Program.cs: `builder.Services.AddScoped<ILobbyService, LobbyService>();`

---

### Step 4: Lobby endpoints (FastEndpoints)

**Create in `fasternfaster.api/Lobby/`:**

| Endpoint | Route | Method | Description |
|----------|-------|--------|-------------|
| `CreateLobbyEndpoint` | `/api/lobbies` | POST | Create public or private lobby |
| `GetLobbyEndpoint` | `/api/lobbies/{id}` | GET | Get lobby details + players |
| `ListLobbiesEndpoint` | `/api/lobbies` | GET | List open public lobbies |
| `JoinLobbyEndpoint` | `/api/lobbies/{id}/join` | POST | Join by lobby ID |
| `JoinByCodeEndpoint` | `/api/lobbies/join-by-code` | POST | Join by invite code |

Each endpoint:
- Has a request DTO and response DTO defined in the same file (or nearby)
- Uses FluentValidation for request validation
- Delegates to `ILobbyService`
- Returns appropriate status codes (201 for create, 404 for not found, 409 for full lobby)

---

### Step 5: Lobby DTOs

**Create in `fasternfaster.api/Lobby/`:**

- `CreateLobbyRequest` — GameMode (required), IsPrivate (default false)
- `CreateLobbyResponse` — Id, GameMode, IsPrivate, InviteCode, Status
- `LobbyResponse` — Id, GameMode, IsPrivate, InviteCode, Status, HostPlayerId, Players[], CreatedAt
- `LobbyPlayerResponse` — Id, DisplayName, JoinOrder, IsConnected
- `JoinLobbyRequest` — DisplayName (required)
- `JoinByCodeRequest` — InviteCode (required), DisplayName (required)

---

### Step 6: Wire SignalR for lobby events

**Modify:** `fasternfaster.api/Infrastructure/Hubs/GameHub.cs`

Add hub methods:
- `JoinLobbyGroup(lobbyId)` — adds connection to a SignalR group named by lobby ID
- `LeaveLobbyGroup(lobbyId)` — removes from group

Server-to-client events (broadcast from service via `IHubContext<GameHub>`):
- `PlayerJoined(lobbyId, player)` — notify lobby group
- `PlayerLeft(lobbyId, playerId)` — notify lobby group
- `HostChanged(lobbyId, newHostPlayerId)` — notify on host promotion
- `LobbyStatusChanged(lobbyId, newStatus)` — notify when race starts

---

### Step 7: Update React app with lobby UI (basic)

**Create:** `fasternfasterapp/src/LobbyList.js` — fetch and display open lobbies
**Create:** `fasternfasterapp/src/CreateLobby.js` — form to create lobby (game mode + private toggle)
**Create:** `fasternfasterapp/src/LobbyRoom.js` — show players in lobby, join/leave, start race (host only)

**Modify:** `fasternfasterapp/src/App.js` — basic routing between lobby list and lobby room

---

## File Summary

| Action | Path |
|--------|------|
| Create | `fasternfaster.api/Lobby/ILobbyService.cs` |
| Create | `fasternfaster.api/Lobby/LobbyService.cs` |
| Create | `fasternfaster.api/Lobby/CreateLobbyEndpoint.cs` |
| Create | `fasternfaster.api/Lobby/GetLobbyEndpoint.cs` |
| Create | `fasternfaster.api/Lobby/ListLobbiesEndpoint.cs` |
| Create | `fasternfaster.api/Lobby/JoinLobbyEndpoint.cs` |
| Create | `fasternfaster.api/Lobby/JoinByCodeEndpoint.cs` |
| Create | `fasternfaster.api/Lobby/LobbyDtos.cs` |
| Modify | `fasternfaster.api/Infrastructure/Hubs/GameHub.cs` |
| Modify | `fasternfaster.api/Program.cs` (register LobbyService) |
| Create | `fasternfasterapp/src/LobbyList.js` |
| Create | `fasternfasterapp/src/CreateLobby.js` |
| Create | `fasternfasterapp/src/LobbyRoom.js` |
| Modify | `fasternfasterapp/src/App.js` |

---

## Done When

- [ ] Migration applied, tables exist in PostgreSQL
- [ ] Full Docker stack runs without errors
- [ ] Can create a public lobby via API (POST `/api/lobbies`)
- [ ] Can create a private lobby and get an invite code back
- [ ] Can list open public lobbies (GET `/api/lobbies`)
- [ ] Can join a lobby by ID or invite code
- [ ] Max 30 players enforced
- [ ] Host promotion works when host leaves
- [ ] SignalR broadcasts player join/leave events to lobby group
- [ ] React app shows lobby list, can create and join a lobby
