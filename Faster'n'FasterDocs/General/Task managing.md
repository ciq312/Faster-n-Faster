---

kanban-plugin: board

---

## To do

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
- [ ] Split Integration tests so that hosts are running separately and doesn't fail because of FastEndpoints.


## In progress



## Bugs

- [ ] ResetPasswordHandler doesn't revoke refresh tokens (ClearActiveSession instead of InvalidateAll)
- [ ] useTyping: keystrokes right after race start get overwritten — needResyncRef is true on every TypingArea mount, first participants broadcast replaces local typed with stale server value → desync, correct chars count as mistakes, stuck at MAX_OVERFLOW


## Done

- [ ] CI never ran the unit tests: backend-build pointed at `fasternfaster.api/FasterNFaster.Api.sln` and `fasternfaster.tests/FasterNFaster.Tests.csproj`, neither of which exists, so restore failed and publish-api/deploy stayed blocked behind it. Repointed at `FasterNFaster.Api.sln` + `FasterNFaster.UnitTests/FasterNFaster.UnitTests.csproj`, dropped the unused Api_project_path/Api_project_name vars, and fixed publish-api's docker context (was `./fasternfaster.api`) to `.` with `file: FasterNFaster.Api.Presentation/Dockerfile`, which is what the Dockerfile's COPY paths assume
- [ ] Git tracked the unit test project as `fasternfaster.UnitTests/` while the folder on disk is `FasterNFaster.UnitTests/` — invisible on Windows because core.ignorecase is true. Renamed the 52 index entries to match disk via `git rm -r --cached` + `git add` (a directory `git mv` failed on a Windows lock, and the index-only route touches no files); .sln now points at the PascalCase path and the stray solution folder wrapping the project is gone. Leave core.ignorecase alone — flipping it on NTFS makes git invent renames
- [ ] InMemoryLobbyRepository singleton shares added/updated/removed lists across lobbies — concurrent saves on different lobbies race
- [ ] Broadcast LobbyState once per lobby change: ILobbyStateTracker collects dirty lobby ids in an AsyncLocal scope, ILobbyStateScope.Run flushes one broadcast per distinct lobby; LobbyAccess.Mutate and RaceAccess.RefreshPassage mark (not RaceAccess.Mutate — ProcessUpdate runs per keystroke). All 9 broadcast sites gone: 7 handlers lost ILobbyQuery, GameHub lost ILobbyRepository/IBroadcaster/ILobbyQuery with its OnDisconnectedAsync broadcast. Scope is opened in exactly two places — LobbyStateFlushBehavior on commands marked ILobbyStateRequest, and RaceFinishedOrchestrationHandler, because GameHub.UpdateRaceState skips MediatR on purpose and that's the path where typing ends a race. Fixes a host disconnect sending 3 identical frames (HostChanged handler + PlayerDisconnected handler + GameHub) and StartRace sending none although it flips IsSessionActive. Both access services stay singleton: LobbyAccess owns the per-lobby semaphores, and MediatREventDispatcher creates a DI scope per event, so a scoped tracker would be a different instance inside every notification handler — AsyncLocal rides ExecutionContext and survives both
- [ ] Race end: RaceSessionEndedEvent carries LobbyId instead of the Lobby aggregate; dropped the now-dead GetRequired in RaceFinishedOrchestrationHandler. The second hop was kept here as a sequencing barrier, then deleted with the LobbyState card above: BroadcastRaceFinishedHandler no longer reads lobby state (RaceEnded's payload comes off the domain event) and the LobbyState read now sits at the end of the same block that does the writes, so MediatR's lack of sibling ordering stopped mattering. RaceFinishedEvent now fans out to SaveRaceResultHandler, RaceFinishedOrchestrationHandler and BroadcastRaceFinishedHandler in any order
- [ ] Race knows its LobbyId (keep Lobby and Race as separate aggregates, referenced by ID, synced via domain events): pass lobbyId to Race constructor, remove IRaceEvent.WrapRaceContext, simplify register/deregister syncing
- [ ] InMemoryLobbyRepository: drop fake unit of work (added/updated/removed lists), plain store; service dispatches events — also move domain event dispatch out of the LobbyAccess gate (SaveChanges dispatches inside the semaphore; SemaphoreSlim is non-reentrant, so any lobby event handler that re-enters Mutate on the same lobby deadlocks)
- [ ] Add WithdrawFromRaceOnPlayerRemovedHandler on PlayerRemovedEvent and drop the race withdrawal branch from DisconnectHandler (rest of "handlers own use cases" done)
- [ ] Collapse race services: remove IRaceInternals → IRaceService (lobby side done — ILobbyAccess + ILobbyQuery, facade deleted)
- [x] Collapse the lobby facade: ILobbyService + ILobbyInternals → ILobbyAccess (Mutate/Create/Remove + reads); ILobbyServiceFacade, LobbyServiceFacade and IRaceTransitionService deleted; GetLobbyStateDTO → ILobbyQuery.GetLobbyState. 25 members across 3 interfaces → 9 across 2. The Internals/Service split was never real — DI handed out the same singleton for both, and the facade injected it twice
- [x] Handlers own use cases: StartSession, KickPlayer, RefreshPassage, RemoveLobbyIfEmpty and RemovePlayerFromLobby inlined into their handlers; BanForCheat composes via DisconnectCommand instead of a shared service; dead withdraw-after-kick branch removed (Lobby.Kick already rejects during a race)
- [x] Cleanup: remove double host validation — StartRace now validates and starts inside one Mutate (was two gate acquisitions and two SaveChanges per race start); RefreshPassage no longer takes the gate for a read-only host check
- [ ] Unit of work: add IUnitOfWork (AppDbContext implements, scoped); repositories only Add/Update (no SaveChanges); handlers commit once — fixes non-atomic ExternalLoginHandler (user + external login saved separately). Update RegisterUser/VerifyEmail/ResetPassword/LinkToExistingAccount, BanRepository, move IStatisticsRepository.SaveAsync + cache invalidation after commit; DB commit before Redis writes
- [ ] Cleanup: single countdown constant (3 in BroadcastRaceStartingHandler vs 3.5 in RaceTickService)
- [ ] Tests: ResetAsync doesn't reset in-memory singletons (sessions, lobbies, location registry, rate limiter)
- [ ] Tests: remove duplicate appsettings.json in IntegrationTests and unused usings
- [ ] Tests: TestApplicationFactory.DisposeAsync — dispose host first (base.DisposeAsync), then DisposeAsync containers instead of StopAsync
- [ ] Tests: RateLimiting window test — dispose WithWebHostBuilder factory, add margin to Task.Delay(window)
- [ ] Tests: dispose HubConnections (`await using`) so they don't leak into the next test
- [ ] Tests: RefreshTokensWhenAccessToExpired_ShouldGiveNew doesn't expire anything or assert new tokens differ; remove unused variables
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