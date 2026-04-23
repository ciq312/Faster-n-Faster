# Task 08 — Google find-or-create via ExternalLogins

## Goal

Given claims from Google (`sub`, `email`, `name`), return an existing `User` or create a fresh one, linked via a row in `ExternalLogins`.

## Files to touch

- **NEW** `fasternfaster.api/Web/Services/IExternalLoginService.cs`:
  ```csharp
  Task<User> FindOrCreateAsync(
      string provider,
      string externalSubject,
      string email,
      string displayName,
      CancellationToken ct);
  ```
- **NEW** `fasternfaster.api/Web/Services/Implementations/ExternalLoginService.cs`:
  1. Lookup `ExternalLogins` by `(provider, externalSubject)`. If found, return the linked `User`.
  2. Else lookup `User` by normalized (lowercase) `Email`. If found, insert a new `ExternalLogin` row linking this user to the provider, return user.
  3. Else create a new `User` with `Email = email`, `Nick = displayName`, `EmailVerified = true`, `Login = null`, `Password = null`. Then insert `ExternalLogin`. Return the new user.
  - Wrap in a transaction (EF `BeginTransactionAsync`) — `(User, ExternalLogin)` must be atomic.
- `fasternfaster.api/Program.cs` — register the service.
- `fasternfaster.api/Web/Users/GoogleAuth/GoogleCallbackEndpoint.cs` — swap its stub call to the real service; hand the returned `User` to `ITokenService.HandlePlayerAuth`.

## Approach

- Google users bypass the email-verification gate because we set `EmailVerified = true` on creation (Google has already verified the email).
- Auto-link (case 2) is safe because Google guarantees the email is verified. A malicious party cannot use Google to hijack an existing password account unless they control the email — in which case they could also do a password reset.
- `Login` and `Password` are left null for Google-only users. If they later want to set a password, that's a separate "add password" flow (out of scope).
- Display-name collision is fine — nicknames are not unique as of task 2.

## Acceptance

- First Google sign-in creates `(User, ExternalLogin)` rows.
- Second Google sign-in (same account) loads the existing user — no new rows.
- Sign in with Google using an email that matches a password-registered user — user is found; a new `ExternalLogin` row appears; no duplicate `User` row is created.
- Transaction rollback on failure (e.g., DB constraint violation on `ExternalLogins` unique index) leaves no orphan `User`.

## Risks / notes

- Normalize emails to lowercase on write (introduced in task 3) so the case-2 lookup works reliably.
- Edge case: email is null on the Google side (rare workspace setup). Handler should reject with a clear error rather than crash on a null email lookup.
