# HTTP Request Error Handling

## Summary

All frontend HTTP requests (guest registration, login, signup, create lobby, refresh) currently ignore server error responses — `fetch` doesn't throw on 4xx/5xx, so the app navigates to `/lobbies` even when the backend returns an error. This feature adds consistent error handling to every `fetch` call and displays error messages to the user on screen.

## Goals

- Check `response.ok` on every HTTP request before processing the response
- Extract error messages from the FastEndpoints error response format
- Display backend error messages visually to the user (not just `console.log`)
- Prevent navigation when a request fails
- Handle network errors (server unreachable) with a user-friendly message

## Non-Goals

- Retry logic or automatic re-authentication
- Global error boundary or centralized HTTP client (keep it simple per-handler for now)
- Backend changes to error responses — FastEndpoints `ThrowError` format is already sufficient
- Toast/notification library — use inline error display

## User Experience

1. User fills in a form (anonymous, login, or signup) and submits
2. **Happy path**: request succeeds → navigate to `/lobbies` as before
3. **Backend error** (e.g., "Login already exists", "Invalid credentials"): an error message appears near the form, form stays on screen, user can fix input and retry
4. **Network error** (server down): a generic "Could not connect to server" message appears
5. Error message clears when the user submits again or switches tabs

## Edge Cases

- Server returns an error response that isn't valid JSON — handle gracefully, show generic message
- Server returns 500 with no useful message — show "Something went wrong, try again"
- Multiple rapid submissions — disable the submit button while a request is in flight to prevent duplicates
- Error message from a previous tab should not persist when switching to another tab (anonymous → login → signup)

## Open Questions

1. Should the error message appear inline below the form, or as a banner at the top of the registration card?
banner
2. Should the submit button show a loading state (spinner/disabled) during the request?
yes
3. Should errors auto-dismiss after a timeout, or stay until the user acts?
auto-dismiss fading out

## Notes

- FastEndpoints `ThrowError` returns errors in this format:
  ```json
  {
    "statusCode": 409,
    "message": "One or more errors occurred.",
    "errors": {
      "GeneralErrors": ["Login already exists"]
    }
  }
  ```
- All three handlers in `Registration.js` need updating: `handleAnonymousSubmit`, `handleLogin`, `handleSignup`
- `handleSignup` currently has no `try/catch` at all
