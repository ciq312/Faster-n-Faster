# CLAUDE.md

- Write clean, testable code following SOLID, YAGNI, DRY, and KISS principles
- Cover new code with tests
- If something is unclear before implementing, ask first

## Commands

Repo root unless noted.

```bash
dotnet build FasterNFaster.Api.sln
dotnet test FasterNFaster.UnitTests           # no Docker needed
dotnet test FasterNFaster.IntegrationTests    # needs Docker running (Testcontainers: Postgres + Redis)
dotnet format FasterNFaster.Api.sln           # CI runs it with --verify-no-changes

cd fasternfasterapp && npm run lint && npm run typecheck && npm run format:check && npm run test:run && npm run build   # what CI runs
cd fasternfasterapp && npm test               # Vitest in watch mode

cp .env.example .env                          # once, then fill in the values; compose and the API read it
docker compose up api --build                 # API in Docker on :8080, starts Postgres and Redis with it
docker compose up postgres redis              # or: only the databases in Docker, and the API on the host:
cd FasterNFaster.Api.Presentation && dotnet watch run   # :8080 via Properties/launchSettings.json; stop the api container first
cd fasternfasterapp && npm run dev            # Vite on :3000, proxies /api and /gameHub to :8080

dotnet ef migrations add <Name> --project FasterNFaster.Api.Infrastructure \
  --startup-project FasterNFaster.Api.Presentation --output-dir Shared/Persistence/Migrations
```

## Layout

- `FasterNFaster.Api.{Domain,UseCases,Infrastructure,Presentation}`: the backend, Clean Architecture ([0003](docs/decisions/0003-clean-architecture-over-layered.md)); folders inside each follow feature areas.
- `FasterNFaster.UnitTests`, `FasterNFaster.IntegrationTests`: xUnit with hand-rolled fakes; integration tests use Testcontainers.
- `fasternfasterapp/`: React + Vite frontend, moving from JavaScript to TypeScript ([0012](docs/decisions/0012-frontend-on-typescript-redux-toolkit-rtk-query.md)): new code is `.ts`/`.tsx`, existing JS stays until converted (`allowJs`).
- `loadtest/`: SignalR load-test bot.

## Architecture docs

- **Where:** `docs/architecture.md` explains how the system runs: where state lives, the main flows, the concurrency model. `docs/decisions/` holds one record per design decision, indexed in `docs/decisions/README.md`. Read the relevant record before changing the mechanism it describes.
- **Keep them true:** a change that alters documented behaviour updates the doc in the same change. A new architectural decision gets a new record from the template in `docs/decisions/README.md`; an accepted record isn't rewritten, it's superseded by a new one.
- **How records are written:** name the principle the decision applies (open-closed, dependency inversion, …) and show the mechanism (the flow, the loop, a short code sketch), not only what the library does.
- **Public repo:** docs cite PRs and commits, never Jira keys.

## Gotchas

- `GameHub.UpdateRaceState` skips MediatR, so pipeline behaviors never run for progress updates. Rules for them live in `RaceParticipant` ([0004](docs/decisions/0004-cqrs-with-mediatr-bypassed-on-the-hot-path.md)).
- A command that changes a lobby must implement `ILobbyStateRequest`, or clients get no `LobbyState` ([0008](docs/decisions/0008-one-lobbystate-broadcast-per-changed-lobby.md)).
- `AggregateGate` isn't re-entrant: code inside `Mutate`/`Read` must not call the same store for the same id ([0006](docs/decisions/0006-stores-as-ports-with-one-gate-per-aggregate.md)).
- A new business error subclasses one of the five `StatusException` categories; add a `code` only when the client branches on it ([0010](docs/decisions/0010-http-status-codes-mapped-in-presentation.md)).
- EF migrations run at API startup and a rollback doesn't undo them, so a schema change must also work with the previous release.
- On the host the API takes its connection strings from `ConnectionStrings__*` lines in `.env`, found by searching up from the working directory. They interpolate `${POSTGRES_*}`, so they must come after those lines, or the values come out empty. In Docker, compose's `environment:` overrides them.

## Git flow

- **Gitflow:** work happens on `feature/<short-meaning>` (no ticket key) branched from an up-to-date `dev`, and goes back by a squash PR into `dev`. `main` only takes release PRs from `dev`. Nothing is edited or committed directly on `dev` or `main`.
- **Conventional Commits** for commit messages and PR titles (`fix(lobby): …`); CI checks PR titles.

## Code style

Single convention, backend and frontend. Check a neighbouring file before adding anything new; if it conflicts with a rule below, the rule below wins — it exists because the codebase had drifted.

- **Private fields (C#):** plain `camelCase`, no leading underscore (`logger`, `budget`, not `_logger`/`_budget`). This is the dominant existing style; a few test files still use `_camelCase` (`_sut`, `_repo`, `_users`) — don't copy that, and fix one in passing if you're already touching that file.
- **Comments:** default to none. Add one only when the code can't explain itself without it — a non-obvious business rule, a reason a design choice must stay a certain way, a trick in a test. Never add a comment that just restates a well-named method or the next line. No section-banner comments (`// -------- happy path --------`).
- **Namespaces:** file-scoped (`namespace X;`), not block-scoped. Already consistent; keep it that way.
- **Blank lines:** single blank line between members at most; no double blank lines.
- **Tests:** backend tests use `Scenario_Expected` naming; frontend `it()` names are sentences without "should" (e.g. `it("rejects a space after a wrong word")`). Extract a named helper for a repeated multi-step setup instead of commenting each call site.
- **Method chains:** leading dot, one call per line once the chain doesn't fit on one line. Prettier enforces this in the frontend; nothing does in C#, so follow it by hand.
- **Formatting is enforced in CI.** Backend: `.editorconfig` rules, checked by `dotnet format --verify-no-changes`. Frontend: Prettier plus the ESLint style rules, checked by `npm run format:check` and `npm run lint`. Fix locally with `dotnet format FasterNFaster.Api.sln` and `npm run format` before pushing.
