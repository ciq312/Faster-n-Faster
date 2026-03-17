# Create Lobby Feature — Implementation Plan

## Architecture Decisions

- **Handler/Command pattern**: GameHub (thin) → Command → Handler (business logic)
- **Reflection-based DI**: `AddHandlers()` scans assembly, auto-registers all `IHandler<,>` implementations
- **GameHub uses `IServiceProvider`**: resolves handlers at runtime, no constructor bloat when adding new handlers
- **Single Lobby entity**: no abstract class / inheritance per game mode — kept simple with nullable `WordCount` and `TimerDurationSeconds`
- **Factory pattern deferred**: only 2 game modes, a switch is fine for now; refactor to factory + reflection when modes grow
- **Web DTOs separate from Commands**: `CreateLobbyRequest` (hub-facing, no ConnectionId) → `CreateLobbyCommand` (internal, includes ConnectionId from hub context)

## Current Structure

```
Core/
  Entities/
    Lobby.cs                — Name, WordCount, TimerDurationSeconds added

UseCases/
  Helpers/
    IHandler.cs             — generic IHandler<TCommand, TResult>
  Lobby/
    CreateLobby/
      Commands/
        CreateLobbyCommand.cs
      Results/
        CreateLobbyResult.cs
      Handlers/
        CreateLobbyHandler.cs

Web/
  DependencyInversion/
    ServiceCollectionExtension.cs — AddHandlers() via reflection
  Hubs/
    Dtos/
      CreateLobbyRequest.cs

Infrastructure/
  Hubs/
    GameHub.cs              — thin, resolves handlers via IServiceProvider
  Data/
    AppDbContext.cs
```

## Completed

- [x] `IHandler<TCommand, TResult>` interface
- [x] `CreateLobbyCommand` record
- [x] `CreateLobbyResult` record
- [x] `CreateLobbyHandler` — creates lobby, player, assigns host, generates invite code if private
- [x] `CreateLobbyRequest` hub DTO
- [x] `GameHub.CreateLobby()` — thin method, resolves handler, sends result to caller, adds to SignalR group
- [x] `ServiceCollectionExtensions.AddHandlers()` — reflection-based auto-registration
- [x] `Lobby` entity updated with Name, WordCount, TimerDurationSeconds
- [x] `Program.cs` updated with `AddHandlers()`
- [x] Build passes — 0 warnings, 0 errors

## Remaining

- [ ] **DB migration** for new Lobby columns (Name, WordCount, TimerDurationSeconds)
- [ ] **AppDbContext update** — configure new columns, unique index on Name for non-finished lobbies
- [ ] **GetLobbies handler** — `GetLobbiesCommand` / `GetLobbiesResult` / `GetLobbiesHandler` returning public waiting lobbies
- [ ] **REST endpoint** for lobby list (GET /api/lobbies) with FastEndpoints
- [ ] **Frontend** — landing page with display name input, create lobby form/modal, lobby list with refresh button

## Spec Decisions (Resolved)

- Lobby name: optional, defaults to "{DisplayName}'s Lobby"
- Duplicate lobby names: not allowed (among non-finished lobbies)
- Max lobby name length: 100 characters
- Lobby list: REST API + manual refresh button (no real-time SignalR for list)
- Display name: inline on main page, no separate welcome screen
- Join by code: deferred to next iteration
- Join button: deferred to next iteration
- Zero-player lobby cleanup: immediate
- Game mode validation: basic positive-number checks only (no strict ranges yet)
- Invite code: 6 chars alphanumeric, collision-checked
