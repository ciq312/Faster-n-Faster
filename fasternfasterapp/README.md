# Faster'n'Faster — Frontend

React SPA for [Faster'n'Faster](https://faster-n-faster.com), a real-time multiplayer typing game. Talks to the ASP.NET Core backend over REST (auth, lobbies, leaderboards) and a single SignalR WebSocket connection (live races).

**Stack:** React 19, Vite, react-router 7, Redux Toolkit + RTK Query, `@microsoft/signalr`, Vitest + Testing Library + MSW. JavaScript moving to TypeScript (strict `tsconfig.json`, `allowJs` until the rest is converted). Plain JSX, CSS per component — no UI framework.

---

## Structure

```
src/
├── app/                 — Redux store and typed hooks
├── features/            — logic grouped by domain
│   ├── auth/            — auth endpoints (login, signup, guest, logout, password reset,
│   │                      email verification), selectors over the /api/auth/me cache
│   │                      (current user, status), Google OAuth hook
│   ├── connection/      — ConnectionProvider: owns the one SignalR connection,
│   │                      exposes invoke/subscribe, handles reconnects and
│   │                      server-pushed session events (kick, ban, second login)
│   ├── game/            — the race itself: typing engine, race/lobby state hooks,
│   │                      typing area, results, player cards
│   ├── lobbies/         — lobby list, create/join
│   ├── leaderboard/     — leaderboard fetch (mockable via VITE_MOCK_LEADERBOARD)
│   └── profile/         — own profile endpoint
├── pages/               — one folder per route
└── shared/              — RTK Query api with refresh-on-401, apiCall fetch wrapper (until
                           lobbies and leaderboard move), banner notifications, navbar
```

Routing: auth pages are public; game routes are nested under `ConnectionLayout`, so the SignalR connection exists only while the user is inside the game area.

---

## Key design

- **Server-authoritative typing, optimistic rendering** — `useTyping` validates every keystroke locally against the passage (mistake tracking, max-overflow cap, no word skipping) for instant feedback, while the server independently verifies all progress. After a reconnect the hook resyncs its state from the server snapshot.
- **Throttled progress updates** — keystroke progress is batched through `useThrottledCallback` before hitting the hub, so fast typists don't flood the connection; the final keystroke flushes immediately.
- **One connection, many subscribers** — components subscribe to hub events through `ConnectionProvider`'s `subscribe()`, which returns an unsubscribe function for effect cleanup. No component owns the connection.
- **Cookie auth end-to-end** — tokens live in HttpOnly cookies; RTK Query requests send credentials, and `baseQueryWithReauth` hits `/api/auth/refresh` once on a 401 (concurrent 401s share that refresh) and retries. The SignalR negotiate request reuses the same cookies.

---

## Development

Requires Node 22 and the backend running on `:8080` (see the [root README](../README.md)).

```bash
npm install
npm run dev        # serves on :3000, proxies /api and /gameHub to :8080
```

The Vite dev proxy keeps everything same-origin, so auth cookies work in dev without CORS setup.

`.env` keys: `VITE_API_URL` (empty = same origin), `VITE_MOCK_LEADERBOARD` (serve mock leaderboard data).

---

## Build & deploy

```bash
npm run build      # outputs dist/
```

The `Dockerfile` is a two-stage build: `node:22-alpine` builds `dist/`, `nginx:alpine` serves it with an SPA fallback (`try_files ... /index.html`). In production, Caddy routes `/api/*` and `/gameHub` to the backend container and everything else here.

Production builds strip `console.log/debug/info/warn` calls and `debugger` statements (see `esbuild.pure` in `vite.config.ts`).
