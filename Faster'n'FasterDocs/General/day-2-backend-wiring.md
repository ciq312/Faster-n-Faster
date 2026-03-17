x# Day 2 Plan — Wiring the Backend & Establishing Real-Time Connection

## Context

Day 1 established the project skeleton: React + ASP.NET Core apps, Docker orchestration, NuGet packages, and a health endpoint. Nothing is actually wired up — Program.cs only registers controllers and Swagger. Day 2 connects all infrastructure (PostgreSQL, Redis, SignalR) and proves the real-time pipeline works end-to-end.

---

## Implementation Steps (in dependency order)

### Step 1: Create API `.env` file

**Create:** `fasternfaster.api/.env`

```
DATABASE_URL=Host=localhost;Port=5432;Database=MyPersonalFastDb;Username=MyDbAdminUser;Password=MyVerySecurePostgresPasswordL
REDIS_URL=localhost:6379
CORS_ORIGINS=http://localhost:3000,http://localhost:80
```

Uses `localhost` for local `dotnet run` against Docker services. Docker overrides these via environment variables in compose.

---

### Step 2: Create EF Core Entity Models

**Create 4 files in `fasternfaster.api/Models/`:**

| File | Table | Key Fields |
|------|-------|------------|
| `Lobby.cs` | `lobbies` | Id (Guid), GameMode, IsPrivate, InviteCode (unique, nullable, 8 char), Status (waiting/racing/finished), HostPlayerId, CreatedAt, UpdatedAt |
| `LobbyPlayer.cs` | `lobby_players` | Id, LobbyId (FK), UserId (nullable for future auth), DisplayName (max 30), JoinOrder, ConnectionId, IsConnected, JoinedAt |
| `RaceResult.cs` | `race_results` | Id, LobbyId (FK), LobbyPlayerId (FK), GrossWpm, NetWpm, Accuracy, MistakeCount, FinishPosition, FinishedAt |
| `CommentThreshold.cs` | `comment_thresholds` | Id, MinWpm, MaxWpm, CommentText (max 500), CooldownSeconds (default 10) |

Design decisions:
- Guid PKs, `snake_case` column names via `[Column]` attributes
- UserId nullable per spec (auth later)
- Both GrossWpm and NetWpm stored (switchable per player preference)
- Navigation properties for EF relationships

---

### Step 3: Create DbContext

**Create:** `fasternfaster.api/Data/AppDbContext.cs`

Using folder name `Data` (not `DbContext`) to avoid namespace collision with `Microsoft.EntityFrameworkCore.DbContext`.

Configures:
- Unique filtered index on `InviteCode` (non-null only)
- Lobby → LobbyPlayers: cascade delete
- RaceResult → Lobby: cascade delete
- RaceResult → LobbyPlayer: restrict (preserve results if player removed)

---

### Step 4: Create SignalR Hub

**Create:** `fasternfaster.api/Hubs/GameHub.cs`

- `OnConnectedAsync` / `OnDisconnectedAsync` — log connection IDs
- `Ping()` — returns `Pong` with UTC timestamp to caller
- `BroadcastMessage(user, message)` — sends `ReceiveBroadcast` to all clients

---

### Step 5: Wire Up Program.cs

**Modify:** `fasternfaster.api/Program.cs`

Order of registration:
1. `Env.Load()` — load .env file
2. `AddDbContext<AppDbContext>` with `UseNpgsql(DATABASE_URL)`
3. `AddSingleton<IConnectionMultiplexer>` — Redis connection
4. `AddSignalR().AddStackExchangeRedis()` — SignalR with Redis backplane
5. `AddCors` — allow configured origins with `AllowCredentials()` (required for SignalR)
6. `AddControllers` + `AddOpenApiDocument` (existing)

Middleware order:
1. `UseCors()` — must be before MapHub/MapControllers
2. `UseOpenApi()` + `UseSwaggerUi()` (existing)
3. `MapControllers()` (existing)
4. `MapHub<GameHub>("/gameHub")` — new

---

### Step 6: Update Nginx for SignalR WebSocket proxying

**Modify:** `fasternfasterapp/nginx.conf`

Add `/gameHub` location block with:
- WebSocket upgrade headers (`Upgrade`, `Connection`)
- Long timeouts (`proxy_read_timeout 86400s`) to keep WebSocket alive

---

### Step 7: Update docker-compose.yml

**Modify:** `docker-compose.yml`

Add `environment` block to `api` service with Docker-internal hostnames:
- `DATABASE_URL=Host=postgres;Port=5432;...`
- `REDIS_URL=redis:6379`
- `CORS_ORIGINS=http://localhost,http://localhost:80`

---

### Step 8: Install SignalR client & update React app

**Run:** `npm install @microsoft/signalr` in `fasternfasterapp/`

**Create:** `fasternfasterapp/src/signalrConnection.js`
- HubConnectionBuilder pointing to `/gameHub`
- Auto-reconnect with backoff: [0, 2000, 5000, 10000, 30000]ms

**Modify:** `fasternfasterapp/src/App.js`
- Show connection status (Connected/Disconnected/Reconnecting/Failed)
- "Ping Hub" button → calls `Ping`, displays `Pong` timestamp
- "Send Broadcast" button → calls `BroadcastMessage`, displays received messages in a list
- Keep existing "Ping API" health check button

**Modify:** `fasternfasterapp/package.json`
- Add `"proxy": "http://localhost:8080"` for CRA dev proxy (local `npm start` routes `/api/` and `/gameHub` to API)

---

### Step 9: Generate & apply EF Core migration

```bash
# From fasternfaster.api/ with PostgreSQL running
dotnet ef migrations add InitialSchema --output-dir Migrations
dotnet ef database update
```

Prerequisite: `dotnet tool install --global dotnet-ef` if not installed.

---

## File Summary

| Action | Path |
|--------|------|
| Create | `fasternfaster.api/.env` |
| Create | `fasternfaster.api/Models/Lobby.cs` |
| Create | `fasternfaster.api/Models/LobbyPlayer.cs` |
| Create | `fasternfaster.api/Models/RaceResult.cs` |
| Create | `fasternfaster.api/Models/CommentThreshold.cs` |
| Create | `fasternfaster.api/Data/AppDbContext.cs` |
| Create | `fasternfaster.api/Hubs/GameHub.cs` |
| Modify | `fasternfaster.api/Program.cs` |
| Modify | `fasternfasterapp/nginx.conf` |
| Modify | `docker-compose.yml` |
| Create | `fasternfasterapp/src/signalrConnection.js` |
| Modify | `fasternfasterapp/src/App.js` |
| Modify | `fasternfasterapp/package.json` |
| Generated | `fasternfaster.api/Migrations/*` (via dotnet ef) |

---

## Verification

1. `docker compose up postgres redis -d` — start DB and cache
2. `dotnet run` from `fasternfaster.api/` — API starts without errors
3. Check Swagger at `http://localhost:8080/swagger`
4. `npm start` from `fasternfasterapp/` — React app at `http://localhost:3000`
5. Page shows "Connected" hub status
6. "Ping Hub" returns UTC timestamp
7. "Send Broadcast" displays message in list
8. Verify tables exist in PostgreSQL: `lobbies`, `lobby_players`, `race_results`, `comment_thresholds`
9. `docker compose up --build` — full stack runs, same tests pass at `http://localhost`

---

## Done When

- [ ] Program.cs loads env config, registers CORS, EF Core, Redis, and SignalR
- [ ] Database schema exists with all four core tables
- [ ] Migration applied successfully to PostgreSQL
- [ ] SignalR hub running with ping/pong and broadcast methods
- [ ] React app connects to SignalR and displays connection status
- [ ] Client can send a message to server and receive real-time response
- [ ] Full stack runs together via Docker without errors
