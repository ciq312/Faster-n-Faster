# Player Caret Color

## Summary

Each player in a lobby gets a unique color for their typing caret, so spectators and participants can visually distinguish who is where in the passage. Colors are assigned automatically on join but can be changed by the player. The server enforces uniqueness within a lobby and broadcasts colors in race state snapshots.

## Goals

- Every player in a lobby has a visually distinct caret color
- Colors are auto-assigned on join (first available from a predefined palette)
- Players can change their color via a request
- Server validates that no two players in the same lobby share a color
- Color is included in race state snapshots so all clients can render opponent carets correctly

## Non-Goals

- Custom/hex color input — players pick from a fixed palette only
- Persisting color preference across sessions or lobbies

## User Experience

1. Player joins a lobby — server assigns the first available color from the palette
2. In the lobby waiting room, the player sees their assigned color on their player card the color can be changed via click on this color then color palette opens
3. Player can optionally change their color — UI shows available (unclaimed) colors
4. If another player already has that color, the server rejects the change
5. During the race, each opponent's caret is rendered in their assigned color
6. Race state snapshots include each participant's color

## Data Flow

- `LobbyPlayer` owns the `Color` property — assigned on join, changeable in waiting
- When a race starts and `RaceParticipant` is created, color is copied from `LobbyPlayer`
- Race state snapshots (broadcast via SignalR) include the color per participant
- Client renders carets using the color from the snapshot

## Edge Cases

- All palette colors are taken (lobby has more players than colors) — cycle/repeat colors with a visual differentiator (e.g., pattern or dimmed variant) number of colors is maximum 
number of players 20 (now it 30 and need to be changed)
- Player disconnects — their color becomes available for new joiners
- Player changes color during countdown — should be rejected once race is starting
- Two players request the same color simultaneously — server lock on lobby ensures only one succeeds
- Host kicks a player — their color is freed

## Open Questions

1. How many colors in the palette? (12? 16? Should match max lobby size of 30?) max lobby size 20 
2. Should color be changeable only in the waiting room, or also between races in the same lobby? between races lobby is waiting room
3. Should the color appear on the LobbyPlayerCard in the waiting room, or only on carets during the race? yes on the card

## Notes

- The palette should be defined as a static list (not DB-configurable) — colors are a UI concern, not a tunable game parameter
- Color assignment logic belongs in `Lobby.AddPlayer` or a method on `LobbyPlayer`
- Consider a `PlayerColors` static class with the palette and an assignment helper true
