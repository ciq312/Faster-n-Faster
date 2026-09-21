---

kanban-plugin: board

---

## To do

- [ ] 1. ChangeColor calls lobbyService directly (line 184) — bypasses MediatR, inconsistent with every other method. Needs a ChangeColorCommand.
- [ ] 2. lobbyStore.GetRequired() called in the hub (lines 118, 130, 239) — the hub is reaching into the store directly to get data for broadcasting. The handler should handle this, or the broadcaster should be smarter.
- [ ] 3. BroadcastLobbyState in the hub after commands (lines 119, 133, 185, 240) — post-command broadcasting is a side effect that belongs in handlers, not the hub. The hub shouldn't need to know "after this command, go broadcast."
- [ ] 4. OnDisconnectedAsync sends two separate commands (lines 233–234) — FastReconnectCommand then DisconnectCommand. This dual orchestration is application logic; a single PlayerDisconnectedCommand handler should coordinate both.


## In progress

- [ ] Refactor


## Bugs



## Done

- [x] Centralize SignalR method names + lobby group key as constants
- [x] Fix public API typos: AddPaticipants, RemoveRegistredRace, GetRaceStatics, playerdId
- [x] Fix namespace≠folder mismatches and FasterNFaster/FasternFaster spelling
- [x] Remove pointless WordRace.Reset() override
- [x] Return LobbyStateDTO from LobbyStateBroadcaster instead of object; stop sending anonymous objects
- [x] Remove misleading async on Lobby.GenerateUniqueInviteCode (no await)
- [x] Encapsulate Lobby.BannedPlayersIds (public field → private set)
- [x] Consistent logging: inject ILogger<T>, use structured templates (drop static Serilog Log + interpolation)
- [x] Fix TOCTOU: reviewed — not a real bug (Lobby is a reference type; IsSessionActive always reads current value)
- [x] Replace fake async (LobbySessionService.StartRaceInternal; GetRaceSettings now sync)
- [x] Use typed exceptions instead of string-matching messages (ConnectToLobby "Can't join")
- [x] DIP: decouple RaceTickService from IHubContext<GameHub> via IRaceBroadcaster (UseCases must not depend on Web/SignalR)
- [x] OCP: make RaceService mode-agnostic — removed `as WordRace` casts; Race.GetPassageWordCount/ApplyPassage virtual; GetRaceType removed
- [ ] Add per-lobby SemaphoreSlim locks in hub/handlers for concurrency safety @{2026-05-04}
- [x] SRP/config: extract IAntiCheatPolicy with configurable thresholds out of RaceParticipant (AntiCheatOptions in appsettings.json)
- [x] SRP: fix LobbySessionService namespace (Services not Interfaces)
- [x] SRP: extract IPlayerLocationRegistry (playerToLobby index) out of LobbyService
- [x] SRP: dedupe ban check (OnConnectedAsync + ConnectToLobby) and centralize `lobby-{id}` group key
- [x] SRP: thin out GameHub — ban+remove extracted to BanForCheatHandler; ILobbySessionService removed from hub
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