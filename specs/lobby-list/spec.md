# Lobby List

## Summary

A lobby list component that displays all available public lobbies. Each lobby entry shows key info and a Join button so players can enter a game. This is a simple, static-data-first UI — no API integration in this iteration.

## Goals

- Display a list of lobbies with relevant info per entry.
- Provide a Join button on each lobby row.
- Keep the component self-contained and ready for API wiring in the next iteration.

## Non-Goals

- API integration — lobby data is hardcoded/mocked for now.
- Join functionality — no Join button in this iteration.
- Filtering, sorting, or searching lobbies.
- Private lobbies or invite-code entry.

## User Experience

1. Player lands on the main page and sees a list of lobbies.
2. Each lobby row is laid out as:
   `[Lobby Name]` ——————————— `[Public/Private]  [playerCount/maxPlayers]  [Join]`
   - **Left**: lobby name.
   - **Right**: access modifier (Public or Private), player count (e.g. "3/30"), then the Join button.
3. No Join button in this iteration — it will be added when join logic is implemented.
4. If there are no lobbies, a simple empty state message is shown (e.g. "No lobbies available").

## Edge Cases

- Empty list: show a clear empty state instead of a blank area.
- Long lobby names: truncate or wrap gracefully — don't break the layout.

## Open Questions

None currently.

## Notes

- Lobby data shape (for mocking): `{ id, name, gameMode, playerCount, maxPlayers, status }`.
- The real data shape comes from `GET /api/lobbies` → `LobbyListItem` — mock should match it so wiring is a drop-in later.
