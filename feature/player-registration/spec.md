# Player Registration

## Summary

When a user opens the site, a modal appears offering two paths: **Register** or **Continue Anonymously**. Registration is not implemented yet (button is visible but non-functional). Anonymous mode generates a UUID and a random display name, stores both in `localStorage`, and lets the player use the app — creating/joining lobbies and navigating between pages with a consistent identity. Registration will be wired up in a future iteration.

## Goals

- Modal on startup with "Register" (disabled/placeholder) and "Continue Anonymously" options
- Anonymous flow: generate a UUID and random display name, store in `localStorage`
- Player identity persists across page navigation and browser sessions (via `localStorage`)
- No database changes — identity is fully client-side for now
- No API calls for this feature

## Non-Goals

- Actual registration or login API
- Database `players` table (deferred — will be added when registration is implemented)
- Password handling, auth tokens, or sessions
- Player statistics

## User Experience

1. User opens the site
2. Modal appears with two options:
   - **Register** — visible but disabled/greyed out with a "Coming soon" hint
   - **Continue Anonymously** — active button
3. User clicks "Continue Anonymously"
4. Client generates a UUID via `crypto.randomUUID()` and a random display name (e.g., "Player_8f2a")
5. Both are saved to `localStorage`
6. Modal closes, user lands on the main page (lobby list)
7. On subsequent visits, `localStorage` is checked — if UUID exists, skip the modal and go straight to the app
8. The display name is used everywhere: lobby creation, lobby list, SignalR connections

## Edge Cases

- User clears `localStorage` — they get a new UUID and name, effectively a new identity. This is acceptable for v1.
- Multiple tabs — same `localStorage`, same identity. No conflict.
- User wants to change their anonymous name — is there a way to edit it, or is it locked?
- `crypto.randomUUID()` browser support — available in all modern browsers, but not in older ones or non-HTTPS contexts

## Open Questions

1. Should the anonymous display name be editable after initial generation, or fixed?
fixed
2. What format for the random name — "Player_xxxx", adjective+noun (e.g., "SwiftFalcon"), or let the user 
pick a name in the anonymous flow? 
Player + UUID
3. Should the old `specs/player-registration-feature/` directory be deleted?
yes


## Notes

- The Register button exists in the UI from day one as a placeholder. When registration is implemented later, it will create a `players` table row and link the UUID to a real account.
- Existing lobby/race tables in the DB remain as-is. This feature doesn't touch the backend at all.
- The UUID stored in `localStorage` will later become the key to associate an anonymous player with a registered account (if they choose to register).
