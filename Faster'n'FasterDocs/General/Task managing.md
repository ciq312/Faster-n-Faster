---

kanban-plugin: board

---

## To do

- [ ] SRP: thin out GameHub — move cheat→ban→remove→abort out of UpdateRaceState into a handler/service
- [x] SRP: dedupe ban check (OnConnectedAsync + ConnectToLobby) and centralize `lobby-{id}` group key
- [ ] SRP: extract IPlayerLocationRegistry (playerToLobby index) out of LobbyService
- [x] SRP: fix LobbySessionService namespace (Services not Interfaces)
- [ ] SRP/config: extract IAntiCheatPolicy with configurable thresholds out of RaceParticipant
- [ ] OCP: make RaceService mode-agnostic — remove `as WordRace` casts so Timer mode can be added
- [x] DIP: decouple RaceTickService from IHubContext<GameHub> via IRaceBroadcaster (UseCases must not depend on Web/SignalR)
- [x] Use typed exceptions instead of string-matching messages (ConnectToLobby "Can't join")
- [x] Replace fake async (LobbySessionService.StartRaceInternal)
- [ ] Fix TOCTOU: LobbySessionService reads IsSessionActive outside the lobby lock
- [x] Consistent logging: inject ILogger<T>, use structured templates (drop static Serilog Log + interpolation)
- [x] Encapsulate Lobby.BannedPlayersIds (public field → private set)
- [x] Remove misleading async on Lobby.GenerateUniqueInviteCode (no await)
- [x] Return LobbyStateDTO from LobbyStateBroadcaster instead of object; stop sending anonymous objects
- [x] Remove pointless WordRace.Reset() override
- [x] Fix namespace≠folder mismatches and FasterNFaster/FasternFaster spelling
- [x] Fix public API typos: AddPaticipants, RemoveRegistredRace, GetRaceStatics, playerdId
- [x] Centralize SignalR method names + lobby group key as constants


## In progress

- [ ] Refactor


## Bugs



## Done

- [ ] Add per-lobby SemaphoreSlim locks in hub/handlers for concurrency safety @{2026-05-04}
- [ ] LobbyService
- [ ] guest race finish @{2026-05-03}
- [x] Silent exception swallow: GameHub.ConnectToLobby only rethrows when message starts with "Can't join" — other errors vanish
- [ ] profile @{2026-05-03}
- [ ] Add registration services @{2026-04-23}
- [ ] apiCall frontend @{2026-04-23}
- [x] AppDbContext registered twice in Program.cs (AddScoped + AddDbContext)
- [ ] Add user cookies @{2026-04-23}
- [x] ILobbySessionService registered twice in Program.cs (lines 100 & 109)
- [ ] make docker images @{2026-04-23}
- [x] NullReferenceException used as control flow (RaceService.GetActiveRace, WordRace passage checks) — use DomainException
- [ ] Add safe passwords in db @{2026-04-23}
- [ ] Fix multi lobby creation @{2026-04-13}
- [ ] Add Player host buttons @{2026-04-13}
- [ ] Switch to vite
- [ ] deconstruction of big hooks @{2026-04-29}
- [ ] Lobby remove when all disconnected @{2026-04-30}
- [ ] double message @{2026-04-30}
- [ ] invite code @{2026-04-30}
- [ ] Caret returns to the start @{2026-04-30}
- [ ] when refresh can no longer operate @{2026-04-30}




%% kanban:settings
```
{"kanban-plugin":"board","list-collapse":[false,false,false,false]}
```
%%