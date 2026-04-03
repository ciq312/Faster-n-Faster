# Leaderboards Page

## Summary

A dedicated page at `/leaderboard` that displays the top 5 players ranked by a selected statistic. Users can re-sort by clicking column headers, and the active sort column is visually highlighted. The page uses the existing `POST /api/leaderboards` endpoint — all sorting happens server-side.

## Goals

- Show a ranked table of players (default 5), defaulting to **BestWPM descending**
- Let users re-sort by any of these columns: **Wins, BestWPM, AvgWPM, AvgAccuracy, WordsTyped, RacesTyped**
- Let users choose how many players to display: **5, 10, 50, or 100**
- Gold/silver/bronze medal styling for ranks 1-3
- Visually indicate which column is currently driving the sort and its direction (asc/desc)
- Each sort change re-fetches from the API (no client-side re-sorting)
- Match the existing app aesthetic: dark graphite theme, amber accents, JetBrains Mono
- Wire into the existing `/leaderboard` navbar link that currently goes nowhere

## Non-Goals

- Pagination or infinite scroll — player count selector (5/10/50/100) is sufficient for v1
- Player profile links or click-through to player detail
- Time-scoped leaderboards (weekly, monthly, all-time toggle)
- Search / filter by player name
- Real-time live-updating leaderboard (manual refresh or re-sort only)

## User Experience

1. User clicks **"leaderboard"** in the navbar.
2. Page loads and immediately fetches the top 5 players sorted by **BestWPM descending**.
3. A table renders with columns: **Rank | Player | Wins | Best WPM | Avg WPM | Avg Accuracy | Words Typed | Races Typed**.
4. The **Best WPM** column header is highlighted (amber) with a descending arrow to show it's the active sort.
5. User clicks the **Wins** column header — the page re-fetches with `Criteria: "Wins", IsDescending: true` and the table updates. The Wins header is now highlighted.
6. User clicks **Wins** again — sort flips to ascending. Arrow indicator changes direction.
7. Clicking a different column resets direction to descending (highest first is the natural expectation for stats).

## Edge Cases

- **No players exist yet**: show an empty state message ("No race data yet — be the first to race!") instead of an empty table.
- **Fewer than 5 players**: display however many exist; don't pad with empty rows.
- **API error / network failure**: show an error banner (reuse existing `ErrorBanner` component) and allow retry.
- **Tied values**: the API returns them in whatever order the DB produces — no special tiebreaker display needed for v1.
- **Invalid criteria string**: shouldn't happen from the UI, but if the API rejects it, surface the error via the banner.
- **Anonymous players**: they have stats too — display them the same as registered players (their `Nick` is their display name).
- **Very long player names**: truncate with ellipsis in the table cell to prevent layout breakage.
- **Loading state**: show a skeleton or spinner while the fetch is in-flight, especially on sort changes, so the user knows something is happening.

## Resolved Questions

1. **AvgAccuracy / AvgWPM in DTO** — Yes, update the backend response DTO to include `AvgAccuracy` and `AvgWPM` so we can display and sort by averages.
2. **Player count** — Configurable via a selector. Options: **5, 10, 50, 100** players. The backend already accepts any count.
3. **Medal styling** — Yes, positions 1-3 get gold/silver/bronze medal styling.
4. **Sort direction toggle** — Clicking the same column toggles between descending and ascending. Clicking a different column defaults to descending.
5. **Rank numbering** — Display a rank number for each row based on the current sort position.

## Notes

- The navbar already links to `/leaderboard` — just needs a route and page component.
- `POST /api/leaderboards` accepts `Criteria` as a raw property name string (e.g., `"BestWPM"`, `"Wins"`). The frontend must send exact property names matching `PlayerStatistics` fields.
- `FilterBuildersUtils.GetTopPlayersFilter` uses expression trees to build dynamic OrderBy — any `PlayerStatistics` property works, but the UI should only expose the curated list above to avoid confusion.
