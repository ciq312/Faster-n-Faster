# CLAUDE.md

- Code in this repo is clean code: each function does one thing and does it well.
- It is object-oriented and follows the SOLID principles.
- It stays simple: YAGNI and KISS.
- Design patterns appear where the problem calls for one, not by default.

## Commands

Repo root unless noted.

```bash
dotnet tool restore                           # once; installs dotnet-ef and the SignalR TypeScript generator a Debug build runs
dotnet build FasterNFaster.Api.sln
dotnet test FasterNFaster.UnitTests           # no Docker needed
dotnet test FasterNFaster.IntegrationTests    # needs Docker running (Testcontainers: Postgres + Redis)
dotnet format FasterNFaster.Api.sln           # CI runs it with --verify-no-changes

cd fasternfasterapp && npm run lint && npm run typecheck && npm run format:check && npm run test:run && npm run build   # what CI runs
cd fasternfasterapp && npm test               # Vitest in watch mode

cp .env.example .env                          # once, then fill in the values; compose and the API read it
docker compose up api --build                 # API in Docker on :8080, starts Postgres, Redis and migrate with it
docker compose up postgres redis migrate      # or: only the databases (migrated) in Docker, and the API on the host:
cd FasterNFaster.Api.Presentation && dotnet watch run   # :8080 via Properties/launchSettings.json; the api container must be stopped
cd fasternfasterapp && npm run dev            # Vite on :3000, proxies /api and /gameHub to :8080

dotnet ef migrations add <Name> --project FasterNFaster.Api.Infrastructure \
  --startup-project FasterNFaster.Api.Presentation --output-dir Shared/Persistence/Migrations
```

## Layout

- `FasterNFaster.Api.{Domain,UseCases,Infrastructure,Presentation}`: the backend, Clean Architecture ([0003](docs/decisions/0003-clean-architecture-over-layered.md)); folders inside each follow feature areas.
- `FasterNFaster.UnitTests`, `FasterNFaster.IntegrationTests`: xUnit, moving from hand-rolled fakes to Moq: new tests mock with Moq and assert with Shouldly, existing fakes and `Assert.*` stay until the test is touched; integration tests use Testcontainers and swap services for mocks in `ConfigureApp`.
- `fasternfasterapp/`: React + Vite frontend, moving from JavaScript to TypeScript ([0012](docs/decisions/0012-frontend-on-typescript-redux-toolkit-rtk-query.md)): new code is `.ts`/`.tsx`, existing JS stays until converted (`allowJs`).
- `loadtest/`: SignalR load-test bot.

## Architecture docs

- **Where:** `docs/architecture.md` explains how the system runs: where state lives, the main flows, the concurrency model. `docs/decisions/` holds one record per design decision, indexed in `docs/decisions/README.md`; each record is the reference for the mechanism it describes.
- **Kept true:** docs change in the same change as the behaviour they describe. A new architectural decision gets a new record from the template in `docs/decisions/README.md`; an accepted record is never rewritten, only superseded by a new one.
- **How records are written:** they name the principle the decision applies (open-closed, dependency inversion, …) and show the mechanism (the flow, the loop, a short code sketch), not only what the library does.
- **Public repo:** docs cite PRs and commits, never Jira keys.

## Gotchas

- `GameHub.UpdateRaceState` skips MediatR, so pipeline behaviors never run for progress updates. Rules for them live in `RaceParticipant` ([0004](docs/decisions/0004-cqrs-with-mediatr-bypassed-on-the-hot-path.md)).
- Only commands that implement `ILobbyStateRequest` trigger a `LobbyState` broadcast; a lobby-changing command without it leaves clients on stale state ([0008](docs/decisions/0008-one-lobbystate-broadcast-per-changed-lobby.md)).
- `AggregateGate` isn't re-entrant: calling the same store for the same id from inside `Mutate`/`Read` deadlocks ([0006](docs/decisions/0006-stores-as-ports-with-one-gate-per-aggregate.md)).
- Business errors subclass one of the five `StatusException` categories; every HTTP error body is `ApiErrorResponse` (`reason`, `code`, `errors`), and an error gets a code other than `error` only when the client branches on it ([0010](docs/decisions/0010-http-status-codes-mapped-in-presentation.md), README's API section).
- EF migrations don't run at API startup: the one-shot `migrate` compose service runs the migration bundle (`efbundle` in the API image) and `api` starts only after it succeeds. A rollback doesn't undo them, so every schema change also works with the previous release.
- Start-up has no side effects before `app.Run()`, because NSwag runs `Program.cs` to generate `swagger.json`: required config is checked with `ValidateOnStart`, never thrown during service registration, and CI passes placeholder values for it on the build step.
- On the host the API takes its connection strings from `ConnectionStrings__*` lines in `.env`, found by searching up from the working directory. They interpolate `${POSTGRES_*}`, so they come after those lines, or the values come out empty. In Docker, compose's `environment:` overrides them.

## Git flow

- **Gitflow:** work happens on `feature/<short-meaning>` (no ticket key) branched from an up-to-date `dev`, and goes back by a squash PR into `dev`. `main` only takes release PRs from `dev`. Nothing is edited or committed directly on `dev` or `main`.
- **Conventional Commits** for commit messages and PR titles (`fix(lobby): …`); CI checks PR titles.

## Code style

Backend and frontend share one convention. Neighbouring files show it; where they disagree with the list below, the list wins, because the codebase had drifted.

- **Private fields (C#):** plain `camelCase`, no leading underscore (`logger`, `budget`, not `_logger`/`_budget`). A few test files still use `_camelCase` (`_sut`, `_repo`, `_users`); those are leftovers, fixed in passing when the file is touched.
- **Comments:** almost none. A comment appears only where the code can't explain itself: a non-obvious business rule, a reason a design choice must stay a certain way, a trick in a test. No comments that restate a well-named method or the next line, and no section-banner comments (`// -------- happy path --------`).
- **Namespaces:** file-scoped (`namespace X;`) everywhere, never block-scoped.
- **Blank lines:** at most one between members; no double blank lines.
- **Tests:** backend tests are named `Scenario_Expected`; frontend `it()` names are sentences without "should" (e.g. `it("rejects a space after a wrong word")`). A repeated multi-step setup lives in a named helper, not in comments at each call site.
- **Method chains:** leading dot, one call per line once the chain doesn't fit on one line. Prettier enforces this in the frontend; in C# nothing does, so it's kept by hand.
- **Endpoint OpenAPI docs:** each endpoint is described in a `<Name>Summary : ApiSummary<TEndpoint>` next to it (summary, description, examples, every status code it actually returns, errors as `ApiErrorResponse`), never in `Configure()`. `ApiSummary` already adds the 500; `ValidationFailed()` and `RateLimited()` add the 400 and 429. The only exception is `Description(x => x.ClearDefaultProduces(…))`, which a summary can't do. Building the API regenerates `swagger.json` and `fasternfasterapp/types.ts`; neither is edited by hand.
- **SignalR contract:** server-to-client events are the methods of `IGameClient` (`UseCases/Realtime/Ports`), sent through `IBroadcaster.Broadcast(audience, c => c.Event(dto))`; client-to-server methods are `IGameHub`, which `GameHub` implements. A new event is a new `IGameClient` method, and its payload DTO gets `[TranspilationSource]`; Domain types never go on the wire directly. A local Debug build regenerates `fasternfasterapp/generated/signalr/` (TypedSignalR), not edited by hand.
- **Formatting is enforced in CI.** Backend: `.editorconfig` rules, checked by `dotnet format --verify-no-changes`. Frontend: Prettier plus the ESLint style rules, checked by `npm run format:check` and `npm run lint`. `dotnet format FasterNFaster.Api.sln` and `npm run format` fix it locally.
