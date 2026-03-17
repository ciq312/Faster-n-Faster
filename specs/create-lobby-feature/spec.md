# Create Lobby Feature

## Summary

Add the ability for players to create and browse lobbies from the main screen. A "Create Lobby" button opens a form where the host configures the lobby (public/private, game mode, word count or timer duration). Created lobbies appear in a lobby list so other players can browse available games. This iteration covers lobby creation and listing only — no join functionality yet.

## Goals

- Let any player create a new lobby and become its host.
- Provide a lobby creation form with settings: lobby name, public/private toggle, game mode (word count vs. timer), and the relevant parameter (word count number or timer duration).
- Display a list of public lobbies fetched via REST API with a manual refresh button.

## Deferred (Next Iteration)

- Allow players to join a lobby from the list (or via invite code for private lobbies).
- Navigate the host (and joining players) into a lobby waiting room after creation/join.
- "Join by Code" input for private lobbies.

## Non-Goals

- **Gameplay / racing UI** — this spec covers lobby creation and listing only, not the typing race itself.
- **Race start flow** — countdown and race initiation are separate features.
- **Player authentication** — players enter a display name only (per v1 design).
- **Lobby search / filtering / sorting** — basic list is sufficient for v1; search can be added later.
- **Lobby editing after creation** — host cannot change settings after the lobby is created in this iteration.

## User Experience

1. **Landing page**: Player sees a display name input, a "Create Lobby" button, a refresh button, and a list of available public lobbies. All on a single page — no separate welcome screen.
2. **Display name**: Player must enter a display name before creating a lobby. The name persists across actions within the session. Entered inline on the main page.
3. **Create Lobby flow**:
   - Player clicks "Create Lobby".
   - A form/modal appears with:
     - **Lobby name** (text input, optional — server generates "{DisplayName}'s Lobby" if left blank, max 100 characters)
     - **Public / Private** toggle (private generates an invite code)
     - **Game mode** selector: "Word Count" or "Timer"
     - **Word count** input (shown when Word Count mode is selected)
     - **Timer duration** input (shown when Timer mode is selected)
   - Player submits → lobby is created on the server → lobby appears in the list. Navigation to a waiting room is deferred.
4. **Lobby list**:
   - Shows public lobbies with: lobby name, host name, player count (e.g., "3/30"), game mode, and status.
   - Only lobbies in "Waiting" status appear in the list.
   - List is fetched via REST API. A "Refresh" button lets players manually reload the list.
   - No join button in this iteration — list is display-only.

## Edge Cases

- **Empty display name**: Block lobby creation until a valid name is entered. Show inline validation.
- **Duplicate lobby names**: Not allowed — server rejects creation if the name is already in use.
- **Private lobby invite code collision**: Server must regenerate the code if a collision occurs (already handled in the entity).
- **Lobby created but host disconnects before anyone joins**: Lobby is cleaned up immediately (no grace period — with zero players there's nothing to preserve).
- **Rapid double-click on Create Lobby**: Prevent duplicate lobby creation (disable button after first click until response).
- **Invalid game mode parameters**: e.g., word count of 0 or negative timer — validate on both client and server. Exact valid ranges are not enforced in this iteration; basic sanity checks only (positive numbers).

## Notes

- The `Lobby` and `LobbyPlayer` entities already exist in the backend with proper status transitions and invite code handling.
- The database schema and migrations are in place — this feature needs the API layer (endpoints, services, DTOs) and the frontend UI.
- SignalR connection with auto-reconnect is already configured on the frontend (will be used in future iterations for in-lobby features, not for the lobby list).
- Per project architecture, endpoints should be thin; lobby creation logic belongs in a service layer behind an interface.
