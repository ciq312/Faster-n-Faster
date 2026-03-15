# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Faster-n-Faster** is a real-time multiplayer typing race web application. Players enter a display name (no auth in v1) and compete in public or private lobbies. Live progress, speed-based taunts/hype comments, and post-race stats (WPM, accuracy, mistakes) are core features.

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | React |
| Backend | ASP.NET Core (C#) |
| Real-time | SignalR (WebSockets) |
| Persistent DB | PostgreSQL |
| Ephemeral / pub-sub | Redis (also SignalR backplane) |

## Architecture

- **Server-authoritative typing validation**: The client only renders visual feedback locally. Every keystroke index is sent to the server, which validates it against the source passage before updating Redis and broadcasting progress. Never trust client-reported progress.
- **Live game state in Redis**: Player positions and progress percentages live in Redis (ephemeral). PostgreSQL stores lobby metadata, completed race results, and `comment_thresholds`.
- **SignalR hub**: Handles connect/disconnect, countdown broadcasts, keystroke validation responses, live comment broadcasts, and race end events. Redis is the SignalR backplane for horizontal scaling.
- **Live comments engine**: Server-side — evaluates per-player WPM against configurable thresholds (stored in PostgreSQL, not hardcoded), broadcasts comment + player name to the whole lobby when a threshold is crossed. Per-player cooldown prevents spam.

## PostgreSQL Schema (planned)

Tables: `lobbies`, `lobby_players`, `race_results`, `comment_thresholds`

- `userId` in player records is nullable — designed so auth can be added later without a full schema rewrite.
- Invite codes for private lobbies: 6–8 character alphanumeric, must be unique (regenerate on collision).

## Key Behavioral Rules

- **Paste is blocked on the client** as a UX convenience, but server validation is the real enforcement layer.
- **Host promotion**: if the host disconnects, promote the next player in the lobby list.
- **Disconnected player mid-race**: cursor freezes, race continues, player excluded from results.
- **Tiebreaker**: server timestamp when two players finish simultaneously.
- **Redis down**: fail gracefully — race unavailable, no silent data corruption.
- **Comment thresholds** must be configurable (DB or config file), not hardcoded, so they can be tuned without redeploy.

## Game Modes (v1)

- **Word count**: type a fixed number of words as fast as possible.
- **Timer**: type as many words as possible within a time limit (e.g., 60s).

## Resolved Design Decisions

- **Max players per lobby**: 30
- **Typing passages**: fixed text initially; random selection from a pool is a future enhancement
- **WPM calculation**: gross vs. net is undecided — design the calculation so it's switchable per player preference
- **Minimum players**: solo play is allowed (no minimum to start a race)
- **Private lobby lifespan**: invite code and lobby persist after race ends so the host can start a rematch
- **Display name validation**: length-limited (exact max TBD); no profanity filter in v1
- **Live comments**: each player sees their own WPM-based comment — comments are personal, not only visible to others
- **Comments UI**: displayed at the top of the page (banner/overlay style)

## Dev Environment Notes

- Local Redis via Docker for development.
- Environment config via `.env`: DB connection strings, Redis URL, CORS origins.
- Docs are in `Faster'n'FasterDocs/General/` (Obsidian vault) — `spec.md` is the authoritative spec.
