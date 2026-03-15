# Faster-n-Faster

A real-time multiplayer typing race web application. Players enter a display name and compete in lobbies by typing a given text as fast and accurately as possible. Live progress of all players is shown during the race, along with auto-generated speed-based comments that taunt or praise players in real time. Results (WPM, accuracy, mistakes) are displayed at the end. v1 ships without accounts — username only.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | React |
| Backend | ASP.NET Core (C#) |
| Real-time | SignalR (WebSockets) |
| Persistent DB | PostgreSQL |
| Ephemeral / pub-sub | Redis |

- **PostgreSQL** stores lobby metadata, completed race results, and player session info.
- **Redis** manages live game state (player positions, progress %) and acts as the SignalR backplane for horizontal scaling.

---

## Features

- Real-time multiplayer typing races with visible competitor progress
- Public and private lobbies (private via invite code)
- Live speed-based comments during a race — taunts and hype messages based on each player's current WPM
- Two game modes: **Word count** (type a fixed number of words) and **Timer** (type as many words as possible in a time limit)
- Post-race stats screen: WPM, accuracy, mistakes, and ranking
- No authentication required — players enter a display name only

---

## How It Works

1. Enter a display name → click **Play**
2. **Quick Join** a random public lobby or **Create Lobby** (pick mode, public/private)
3. Private lobbies generate a shareable invite code
4. Lobby waiting room shows connected players — host starts the race
5. Countdown (3-2-1-Go), then type the displayed passage
6. All player progress updates in real time via SignalR
7. Live comments pop up based on each player's current WPM
8. Race ends when the first player finishes (or time runs out)
9. Results screen shows final ranking, WPM, accuracy, and mistakes
10. Host can start a rematch

---

## Architecture Notes

- **Server-authoritative validation**: every keystroke index is sent to the server and validated against the source passage before progress is updated in Redis and broadcast. The client only renders visual feedback locally.
- **Live game state in Redis**: player positions and progress percentages are ephemeral. PostgreSQL stores lobby metadata, race results, and comment thresholds.
- **Live comments engine**: evaluated server-side against configurable WPM thresholds (stored in PostgreSQL, not hardcoded). Per-player cooldown prevents spam.
- **Host promotion**: if the host disconnects, the next player in the lobby list is promoted.
- **Disconnected players mid-race**: cursor freezes, race continues, player excluded from results.
- **Redis down**: fails gracefully — race unavailable, no silent data corruption.

---

## Dev Setup

Requirements: Node.js, .NET SDK, Docker (for local Redis and PostgreSQL)

```bash
# Start Redis and PostgreSQL via Docker
docker compose up -d

# Run the backend
cd FasterNFaster.Api
dotnet run

# Run the frontend
cd client
npm install
npm run dev
```

Configure environment via `.env`:
- `DATABASE_URL` — PostgreSQL connection string
- `REDIS_URL` — Redis connection string
- `CORS_ORIGINS` — allowed origins for the frontend

---

## Roadmap (Post v1)

- User accounts and persistent profiles
- Global leaderboard / ELO ranking
- Random passage selection from a pool
- Spectator mode
- Mobile-first design
