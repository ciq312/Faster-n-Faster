---

kanban-plugin: board

---

## To do

- [ ] Tests: RefreshTokensWhenAccessToExpired_ShouldGiveNew doesn't expire anything or assert new tokens differ; remove unused variables
- [ ] Tests: dispose HubConnections (`await using`) so they don't leak into the next test
- [ ] Tests: TestApplicationFactory.DisposeAsync — dispose host first (base.DisposeAsync), then DisposeAsync containers instead of StopAsync
- [ ] Tests: ResetAsync doesn't reset in-memory singletons (sessions, lobbies, location registry, rate limiter)
- [ ] Tests: RateLimiting window test — dispose WithWebHostBuilder factory, add margin to Task.Delay(window)
- [ ] Tests: remove duplicate appsettings.json in IntegrationTests and unused usings
- [ ] Cleanup: single countdown constant (3 in BroadcastRaceStartingHandler vs 3.5 in RaceTickService)
- [ ] Cleanup: remove double host validation (facade ValidateHost via WithLobby + StartSession validates again)
- [ ] InMemoryLobbyRepository: drop fake unit of work (added/updated/removed lists), plain store; service dispatches events
- [ ] Collapse lobby/race services: remove ILobbyInternals, IRaceInternals, IRaceTransitionService, ILobbyServiceFacade → ILobbyService + IRaceService + one orchestrator
- [ ] Decide handlers vs services as use-case owners; remove pass-through handlers and the duplicate UpdateProgress path
- [ ] Race knows its LobbyId (or Lobby owns Race): remove IRaceEvent.WrapRaceContext, second race lock, register/deregister syncing
- [ ] Race end: remove second notification (RaceSessionEndedEvent) hop
- [ ] Broadcast LobbyState once per lobby change instead of from every handler
- [ ] Broadcasting: merge IBroadcaster + IRaceBroadcaster, replace IAudience hierarchy with ToLobby/ToPlayer methods, merge GameEvents + GameHubConstants.Methods
- [ ] Remove UserFactory: take Id/Nick from JWT claims in JoinLobby
- [ ] Auth: single source for token lifetimes (JwtOptions vs AuthCookiesOptions), use cookie name option in AuthExtensions
- [ ] Auth: dedupe JwtTokenFactory token methods and CookieWriter cookie writers
- [ ] Auth: merge ResendVerification/RequestPasswordReset flows into shared token issuer; drop redundant RemoveAllForUser before Add
- [ ] Options: merge VerifyEmailOptions/ResetPasswordOptions; stop double-registering cooldown options as raw singletons
- [ ] Caching: drop ban/statistics caching decorators and reflection CacheSerializer; keep leaderboard cache only
- [ ] RaceStateConflator: merge partial files (LobbyBroadcast.cs, RaceFrame.cs) into one
- [ ] PendingRemovalRegistry: make interface synchronous
- [ ] YAGNI: decide on abstract Race + IRaceSettings polymorphism with a single WordRace
- [ ] Frontend: remove nonexistent hub calls (ChangeGameMode, ChangeWordCount, ChangeTimerDuration) and timer mode rendering in Lobby.jsx


## In progress

- [ ] Refactor


## Bugs

- [ ] InMemoryLobbyRepository singleton shares added/updated/removed lists across lobbies — concurrent saves on different lobbies race
- [ ] ResetPasswordHandler doesn't revoke refresh tokens (ClearActiveSession instead of InvalidateAll)


## Done

- [ ] Tests: RefreshWithStaleToken_Should401 passes for the wrong reason (Cookie.ToString() gives `refresh_token=refresh_token=...`) — use `.Value`, assert first refresh is 200
- [ ] Tests: replace Task.Delay waits in HubTests with TaskCompletionSource + WaitAsync timeout (AnotherSessionStarted, hub.Closed); drop unsynchronized bool
- [ ] Cleanup: remove redundant `private readonly x = x;` fields next to primary constructors
- [ ] Cleanup: remove dead code (DisconnectResult, JoinLobbyResult, StartRaceResult, Infrastructure FakeBanRepository, unused locals in LobbyService, ValidGameModes, `await ValueTask.FromResult`, commented tier logic in useRace)
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