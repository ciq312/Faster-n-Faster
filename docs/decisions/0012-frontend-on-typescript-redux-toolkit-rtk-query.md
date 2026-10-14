# 0012. Frontend on TypeScript, Redux Toolkit and RTK Query

- **Status:** Accepted, not started
- **Date:** 2026-10-13

## Context

The frontend is about 3,200 lines of JavaScript/JSX (React 19, Vite, SignalR) with no tests.

- **The backend contract is implicit.** Hub event payloads (`LobbyState`, `RaceState`, `RaceEnded`, …), hub method names (`"ConnectToLobby"`, `"RefreshLobby"`) and HTTP DTOs are untyped strings and objects, so a renamed backend field breaks the client silently.
- **REST hooks are hand-rolled.** Each one manages its own `loading`/`error`/`data`. Half use raw `fetch` instead of `apiCall` and skip the refresh-on-401 retry, and stale responses can overwrite newer ones.
- **Game state is scattered.** `useRace` keeps a race state machine in seven `useState`s. `useLobby` and `useRace` each subscribe to `LobbyState` separately.

## Decision

Move the frontend to TypeScript, a Redux Toolkit store and RTK Query, with tests for every piece that changes. In order:

1. Test runner (Vitest + Testing Library) and TypeScript tooling: strict `tsconfig`, typescript-eslint, a typecheck in CI.
2. A typed backend contract: hub events, hub methods and HTTP DTOs.
3. The store and an RTK Query base query with re-auth; auth moves onto it.
4. Lobbies, leaderboard and profile fetching on RTK Query.
5. The SignalR connection as Redux middleware, and lobby state in a lobby slice. Server events map onto actions and reducers.
6. The race as an explicit state machine in a race slice.
7. Convert the remaining code and turn off `allowJs`.

New code is TypeScript from step 1. Hooks that a later step deletes aren't converted first.

## Alternatives considered

- **TanStack Query for server data.** Rejected so the app has one cache layer, not two: RTK Query lives in the same store as the game state.
- **Next.js.** Undecided as of 2026-10-13. The app is a client-only SignalR app with cookie auth handled by .NET and a static nginx deploy, so server rendering would mainly help public, shareable pages. Everything above carries over unchanged except the Vite-specific tooling.

Part of the reason for this stack is to show it in a portfolio project.

## Consequences

- Backend contract changes surface as type errors instead of runtime bugs.
- One place for server data, caching and refresh handling.
- A larger dependency footprint and more boilerplate than plain hooks.
- Until step 7, the frontend is a mix of JavaScript and TypeScript.
