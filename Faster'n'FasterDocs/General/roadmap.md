# Faster-n-Faster — Development Roadmap (~1 Month)

---

## Week 1 — Infrastructure & Project Setup
**Goal:** Running skeleton with real-time connection established.

| Task | Details |
|---|---|
| Repo & project scaffold | React app + ASP.NET Core API side-by-side |
| PostgreSQL schema | Tables: `lobbies`, `lobby_players`, `race_results`, `comment_thresholds` |
| Redis setup | Local dev via Docker; configure as SignalR backplane |
| SignalR hub (skeleton) | Connect/disconnect events, basic message broadcast |
| React → SignalR connection | Client connects, receives a ping, displays connection status |

**Milestone:** Client connects to backend over WebSocket and receives a real-time message.

---

## Week 2 — Lobby System
**Goal:** Players can create, join, and wait in lobbies.

| Task | Details |
|---|---|
| Display name entry | Home screen, name stored in session/local state |
| Create lobby | `POST /api/lobbies` → returns lobby ID + invite code (if private) |
| Join lobby (public) | `GET /api/lobbies/available` → list + quick join |
| Join lobby (private) | Join via invite code |
| Lobby waiting room UI | Shows connected players, updates in real time via SignalR |
| Host controls | Start race button (host only); host promotion on disconnect |
| Lobby cleanup | Expire empty lobbies after timeout via Redis TTL |

**Milestone:** Two browser tabs can join the same lobby and see each other's names update live.

---

## Week 3 — Core Gameplay + Live Comments
**Goal:** Full race loop from countdown to results, with live comments firing during the race.

| Task | Details |
|---|---|
| Text pool | Seed PostgreSQL with typing passages; random selection on race start |
| Countdown | Server-driven 3-2-1 broadcast via SignalR |
| Typing input | Client handles visual feedback only — no trusted state on client |
| Progress sync | Client sends keystroke index; server validates against passage, updates Redis, broadcasts |
| Live progress UI | Progress bars / cursor indicators per player |
| WPM calculation (live) | Computed server-side from validated keystrokes |
| Live comments engine | Server evaluates WPM against thresholds, broadcasts comment to lobby |
| Comments UI | Banner/overlay at top of page — player name + comment text |
| Race end detection | Server detects first finisher or timer expiry, broadcasts end event |
| WPM & accuracy (final) | Computed server-side at race end |
| Results screen | Ranked list with WPM, accuracy, mistakes per player |
| Rematch | Host can restart; same players rejoin the lobby |

**Milestone:** Two players can complete a full race, see live comments fire, and view results.

---

## Week 4 — Polish & Deployment
**Goal:** Shippable, stable v1.

| Task | Details |
|---|---|
| Error & disconnect handling | Graceful UI for disconnects, lobby-not-found, race-in-progress |
| Input edge cases | Block paste on client (UX only); server validation is real enforcement |
| Comment tuning | Adjust WPM thresholds and cooldowns based on playtesting |
| UI polish | Consistent styling, loading states |
| End-to-end testing | Create lobby → race → results → rematch → disconnect → comment triggers |
| Deployment | Backend on VPS/cloud; frontend on Vercel/Netlify or same host |
| Environment config | `.env` for DB connection strings, Redis URL, CORS origins |

**Milestone:** App is live and accessible via a public URL.
