# Task 09 — Account switching

## Goal

If a request carries a valid JWT for user A and the user logs in as B (email/password or Google), invalidate all of A's sessions and issue fresh credentials for B.

## Files to touch

- `fasternfaster.api/Web/Services/Implementations/InMemorySessionsService.cs` — if not already present, add:
  ```csharp
  void InvalidateAll(Guid userId);
  ```
  Removes all entries for `userId` from both the session dictionary and `TokenStore` (refresh tokens).
- `fasternfaster.api/Web/Services/ISessionService.cs` — expose `InvalidateAll`.
- `fasternfaster.api/Web/Users/LoginUser/EndPoints/LoginUserEndpoint.cs` — before calling `HandlePlayerAuth`, inspect the incoming JWT (if any). If present and valid, extract `userId` and call `sessions.InvalidateAll(oldUserId)`. Do this whether or not `oldUserId == newUserId` — simpler and safer.
- `fasternfaster.api/Web/Users/GoogleAuth/GoogleCallbackEndpoint.cs` — same treatment.
- **Frontend**:
  - `fasternfasterapp/src/features/auth/hooks/useLogin.js` — on 200 success, clear any existing `localStorage` auth state (userId, nick, etc.) before writing the new user's data. Prevents the stale-old-user-under-new-token torn state.
  - `fasternfasterapp/src/pages/AuthCallback/AuthCallback.js` — same: clear old state before bootstrapping the new session.

## Approach

- "Inspect the incoming JWT" = read the `Authorization` header or auth cookie; if `HttpContext.User.Identity.IsAuthenticated`, take `userId` from the `sub` claim.
- Invalidate *all* of old-user's sessions, not just the current one — the common failure mode is "logged in on another tab" and we don't want stale tokens floating around under the old identity.
- Don't revoke the new user's existing sessions (someone else on another device shouldn't be kicked).

## Acceptance

- Logged in as Alice, call `/api/auth/login` with Bob's creds → Bob's JWT is issued, Alice's refresh tokens no longer work (next refresh returns 401).
- Logged in as Alice, Google sign-in as Charlie → Charlie's session is active, Alice's refresh tokens are gone.
- Same user re-logging in (Alice → Alice) still works — old session is invalidated, new one is issued.

## Risks / notes

- Guest sessions (`RegisterAnonymous`) — switching from guest to registered should also clear the guest token. Confirm `InvalidateAll(guestUserId)` covers this.
- If sessions move to Redis later, `InvalidateAll` semantics must be preserved.
