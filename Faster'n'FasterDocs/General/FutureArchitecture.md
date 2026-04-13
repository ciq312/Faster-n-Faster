## Architecture — make concurrent lobbies 
Currently if thread enters to change lobby state and another thread already locked it the thread is blocked and waiting for it to finish. It should be made asyncronously so that thread is freed when locked.
## Architecture — split race construction from lobby creation

Current state (v1, intentionally simplified): `CreateLobbyHandler` builds the `WordRace`, loads its passage, and creates the lobby all in one handler. Only WordRace is supported.

Target design (do when TimerRace is added, or when host needs to change race mode in an existing lobby):

- **`WordRaceBuilder` / `TimerRaceBuilder`** — one class per race type, each owns its own construction (params + passage loading). Concrete classes, no shared interface until a consumer actually needs polymorphism.
- **`LobbyCreator`** — generic lobby creation: name, privacy, host, invite code, persistence. Takes an already-built `Race` as input, doesn't care which subtype.
- **Per-mode create handlers** (`CreateWordLobbyHandler`, `CreateTimerLobbyHandler`) — thin: call the matching builder, hand the race to `LobbyCreator`, return the result.
- **Per-mode change handlers** (`ChangeToWordRaceHandler`, `ChangeToTimerRaceHandler`) — reuse the same builders to swap the race on an existing lobby via `Lobby.SetInitialRace(...)`. This is the main feature unlocked by the split.

Why deferred: with only WordRace today, the full structure is more moving parts than the feature justifies. Once a second race mode or a race-swap UX appears, the duplication pressure becomes real and this refactor pays for itself.

Also drop at that time: `IRaceSettings` (empty marker), `Race.GetRaceSettings()`, and any nested settings records — they're the wrong abstraction. Wire-format settings belong in a sealed DTO hierarchy in `UseCases`/`Web`, not on the domain entity.