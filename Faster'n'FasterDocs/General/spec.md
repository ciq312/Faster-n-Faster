# Faster-n-Faster

## Summary

A real-time multiplayer typing race web application. Players enter a display name and compete in lobbies by typing a given text as fast and accurately as possible. Live progress of all players is shown during the race, along with auto-generated speed-based comments that taunt or praise players in real time. Results (WPM, accuracy, mistakes) are displayed at the end. v1 ships without accounts — username only, with auth planned for a later phase.

---

## Goals

- Real-time multiplayer typing races with visible competitor progress
- Public and private lobbies (private via invite code)
- Live speed-based comments shown during a race (taunts and hype messages based on current WPM)
- At least one game mode (word count or timer-based)
- Post-race stats screen (WPM, accuracy, mistakes, ranking)
- No authentication required in v1 — players enter a display name only

## Non-Goals (v1)

- User accounts, login, or persistent profiles
- Leaderboard / global rankings
- Ranked / ELO rating system
- Mobile-first design (responsive is nice-to-have, not required)
- Spectator mode

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | React |
| Backend | ASP.NET Core (C#) |
| Real-time | SignalR (WebSockets) |
| Persistent DB | PostgreSQL |
| Ephemeral / pub-sub | Redis |

**Why PostgreSQL + Redis:**
- PostgreSQL stores lobby metadata, completed race results, and player session info.
- Redis manages live game state (player positions, progress %) and SignalR backplane for horizontal scaling.

---

## User Experience

### Happy Path

1. User lands on home page → enters a display name → clicks **Play**.
2. User sees two options: **Quick Join** (random public lobby) or **Create Lobby**.
3. **Create Lobby**: user picks game mode, sets public/private, gets a shareable invite code if private.
4. Lobby waiting room shows connected players. Host can start the race.
5. Countdown (3-2-1-Go), then the text to type appears.
6. As players type, their progress bars / cursors update in real time for all participants.
7. **Live comments** pop up during the race based on each player's current WPM (see below).
8. Race ends when the first player finishes (or timer runs out in timer mode).
9. Results screen shows final ranking, WPM, accuracy, and mistakes for each player.
10. Host can start a rematch or players can leave.

### Live Speed-Based Comments

Comments are shown in real time during a race, triggered by a player's current WPM crossing thresholds. Examples from the design doc:

| Trigger | Example Comment |
|---|---|
| Very slow WPM | "You are typing slower than my grandma" |
| Average WPM | (neutral / no comment) |
| Fast WPM | "You are insane" |
| Very fast WPM | "You are not a human" |
| Exceptional WPM | "Are you a God?" |

- Comments are visible to **all players in the lobby**, showing which player triggered them.
- Thresholds and comment text should be configurable (not hardcoded) so they can be tuned easily.
- Comments should not fire too frequently — a cooldown per player prevents spam.

### Game Modes (v1 — pick at least one)

- **Word count**: type a fixed number of words as fast as possible.
- **Timer**: type as many words as possible within a time limit (e.g., 60s).

---

## Edge Cases

- Player disconnects mid-race → their cursor freezes; race continues for others; disconnected player is removed from results.
- Player joins a lobby that just started → they are queued for the next round (or shown a "race in progress" screen).
- All players disconnect before race starts → lobby is cleaned up from Redis/PostgreSQL after a timeout.
- Two players finish at the exact same millisecond → server timestamp is the tiebreaker.
- Private lobby invite code collision → codes must be unique; regenerate on collision.
- Player pastes text or sends manipulated progress → server validates every keystroke index against the source passage; invalid/out-of-order submissions are rejected and not broadcast.
- Lobby host leaves → promote the next player in the list to host.
- Redis goes down → backend should fail gracefully (race unavailable, no silent data corruption).
- Live comment spam → per-player cooldown prevents the same player triggering a comment too frequently.
- Multiple players at the same WPM threshold simultaneously → comments should be queued/staggered to avoid flooding the UI.

---

## Open Questions

1. What is the maximum number of players per lobby? (Affects real-time update frequency and UI layout.)
2. Should game text (the passage to type) be randomly selected from a pool, or fixed per game mode?
3. Should WPM be calculated gross or net (i.e., do mistakes reduce WPM)?
4. Is there a minimum player count to start a race, or can a solo player race against themselves?
5. What happens to a private lobby after the race ends — does the invite code persist for a rematch?
6. Should the display name be validated (length, profanity filter)?
7. Are live comments shown to the player who triggered them, or only to other players?
8. Should comments be shown as a toast/overlay on screen or in a dedicated side panel?

---

## Implementation Plan (~1 Month)

### Week 1 — Infrastructure & Project Setup
**Goal:** Running skeleton with real-time connection established.

| Task | Details |
|---|---|
| Repo & project scaffold | React app + ASP.NET Core API in one repo (monorepo or side-by-side) |
| PostgreSQL schema | Tables: `lobbies`, `lobby_players`, `race_results`, `comment_thresholds` |
| Redis setup | Local dev via Docker; configure as SignalR backplane |
| SignalR hub (skeleton) | Connect/disconnect events, basic message broadcast |
| React → SignalR connection | Client connects, receives a ping, displays connection status |

**Milestone:** Client connects to backend over WebSocket and receives a real-time message.

---

### Week 2 — Lobby System
**Goal:** Players can create, join, and wait in lobbies.

| Task | Details |
|---|---|
| Display name entry | Home screen, name stored in session/local state |
| Create lobby | POST `/api/lobbies` → returns lobby ID + invite code (if private) |
| Join lobby (public) | GET `/api/lobbies/available` → list + quick join |
| Join lobby (private) | Join via invite code |
| Lobby waiting room UI | Shows connected players, updates in real time via SignalR |
| Host controls | Start race button (host only); host promotion on disconnect |
| Lobby cleanup | Expire empty lobbies after timeout via Redis TTL |

**Milestone:** Two browser tabs can join the same lobby and see each other's names update live.

---

### Week 3 — Core Gameplay + Live Comments
**Goal:** Full race loop from countdown to results, with live comments firing during the race.

| Task | Details |
|---|---|
| Text pool | Seed PostgreSQL with typing passages; random selection on race start |
| Countdown | Server-driven 3-2-1 broadcast via SignalR |
| Typing input | Client handles visual feedback only (highlight errors, move cursor) — no trusted state lives on the client |
| Progress sync | Client sends keystroke index to server; **server validates** it matches the expected character in the passage before updating progress in Redis and broadcasting to other players |
| Live progress UI | Progress bars or cursor indicators per player, real-time updates |
| WPM calculation (live) | Computed server-side from validated keystrokes; sent back to client for display and comment triggering |
| Live comments engine | Server holds threshold config; evaluates WPM server-side and broadcasts comment to the lobby when threshold is crossed |
| Comments UI | Toast / overlay showing player name + comment text |
| Race end detection | Server detects first finisher (or timer expiry); broadcasts end event |
| WPM & accuracy calc (final) | Computed server-side at race end |
| Results screen | Ranked list with WPM, accuracy, mistakes per player |
| Rematch | Host can restart; same players rejoin the lobby |

**Milestone:** Two players can complete a full race, see live comments fire, and view results.

---

### Week 4 — Polish & Deployment
**Goal:** Shippable, stable v1.

| Task | Details |
|---|---|
| Error & disconnect handling | Graceful UI for disconnects, lobby-not-found, race-in-progress |
| Input edge cases | Block paste on client as UX convenience; server validation is the real enforcement layer |
| Comment tuning | Adjust WPM thresholds and cooldowns based on playtesting |
| UI polish | Responsive layout, consistent styling, loading states |
| End-to-end testing | Manual test: create lobby, race, results, rematch, disconnect, comment triggers |
| Deployment | Backend on a VPS/cloud (e.g., Azure, Railway, Fly.io); frontend on Vercel/Netlify or same host |
| Environment config | `.env` for DB connection strings, Redis URL, CORS origins |

**Milestone:** App is live and accessible via a public URL.

---

## Notes

- SignalR is the natural choice for real-time in ASP.NET Core — it handles WebSocket fallback automatically and integrates with Redis as a backplane for multi-instance deployments.
- Auth (accounts, persistent stats, ranked mode) is intentionally deferred to a future phase. The display name flow should be designed so adding auth later doesn't require a full rewrite (e.g., a `userId` field can be nullable in the DB).
- Invite code for private lobbies: 6–8 character alphanumeric codes are readable and collision-resistant enough for this scale.
- Live comment thresholds should be stored in PostgreSQL (or a config file) rather than hardcoded so they can be adjusted without a redeploy.
- **Typing validation is server-authoritative.** The client only renders visual feedback locally; all progress that gets shared with other players is validated server-side. This prevents cheating via dev console or modified requests. The trade-off is a small latency round-trip per keystroke for shared state — acceptable given the nature of the game.
