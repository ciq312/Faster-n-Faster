---

kanban-plugin: board

---

## To do

- [ ] lobby leave
- [ ] progress save
- [ ] lobby clear
- [ ] Redesign
- [ ] Program refactor
- [ ] SRP: thin out GameHub — move cheat→ban→remove→abort out of UpdateRaceState into a handler/service
- [ ] SRP: dedupe ban check (OnConnectedAsync + ConnectToLobby) and centralize `lobby-{id}` group key
- [ ] SRP: extract IPlayerLocationRegistry (playerToLobby index) out of LobbyService
- [ ] SRP: fix LobbySessionService namespace (Services not Interfaces); split ILobbySessionService / IRaceTransitionService roles
- [ ] SRP/config: extract IAntiCheatPolicy with configurable thresholds out of RaceParticipant
- [ ] OCP: make RaceService mode-agnostic — remove `as WordRace` casts so Timer mode can be added
- [ ] DIP: decouple RaceTickService from IHubContext<GameHub> via IRaceBroadcaster (UseCases must not depend on Web/SignalR)
- [ ] Use typed exceptions instead of string-matching messages (ConnectToLobby "Can't join")
- [ ] Replace fake async (Task.FromResult/Task.CompletedTask over sync work) in RaceService/LobbySessionService
- [ ] Fix TOCTOU: LobbySessionService reads IsSessionActive outside the lobby lock
- [ ] Consistent logging: inject ILogger<T>, use structured templates (drop static Serilog Log + interpolation)
- [ ] Encapsulate Lobby.BannedPlayersIds (public field → private set)
- [ ] Remove misleading async on Lobby.GenerateUniqueInviteCode (no await)
- [ ] Return LobbyStateDTO from LobbyStateBroadcaster instead of object; stop sending anonymous objects
- [ ] Remove pointless WordRace.Reset() override
- [ ] Fix namespace≠folder mismatches and FasterNFaster/FasternFaster spelling
- [ ] Fix public API typos: AddPaticipants, RemoveRegistredRace, GetRaceStatics, playerdId
- [ ] Centralize SignalR method names + lobby group key as constants


## In progress

- [ ] Refactor


## Bugs

- [x] Silent exception swallow: GameHub.ConnectToLobby only rethrows when message starts with "Can't join" — other errors vanish
- [x] AppDbContext registered twice in Program.cs (AddScoped + AddDbContext)
- [x] ILobbySessionService registered twice in Program.cs (lines 100 & 109)
- [x] NullReferenceException used as control flow (RaceService.GetActiveRace, WordRace passage checks) — use DomainException



## Done

- [ ] Add per-lobby SemaphoreSlim locks in hub/handlers for concurrency safety @{2026-05-04}
- [ ] LobbyService
- [ ] guest race finish @{2026-05-03}
- [ ] profile @{2026-05-03}
- [ ] Add registration services @{2026-04-23}
- [ ] apiCall frontend @{2026-04-23}
- [ ] Add user cookies @{2026-04-23}
- [ ] make docker images @{2026-04-23}
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