# Task 07 — Google OAuth endpoint (unified)

## Goal

Replace the stub `/api/auth/register/google` with a unified `/api/auth/google` that handles both first-time and returning Google users via a challenge + callback pair.

## Files to touch

- Delete `fasternfaster.api/Web/Users/RegisterUser/EndPoints/GoogleRegisterUserEndpoint.cs`.
- **NEW** `fasternfaster.api/Web/Users/GoogleAuth/GoogleChallengeEndpoint.cs`:
  - GET `/api/auth/google?returnUrl=...`.
  - Build `AuthenticationProperties { RedirectUri = "/api/auth/google/callback", Items["returnUrl"] = returnUrl }`.
  - Return `Results.Challenge(props, new[] { "Google" })`.
- **NEW** `fasternfaster.api/Web/Users/GoogleAuth/GoogleCallbackEndpoint.cs`:
  - GET `/api/auth/google/callback`.
  - `var result = await HttpContext.AuthenticateAsync("External");` — reads claims from the external cookie.
  - Extract `sub` (NameIdentifier), `email`, `name` from `result.Principal.Claims`.
  - Call `IExternalLoginService.FindOrCreateAsync("google", sub, email, name, ct)` (task 8 — stub returning a `User` is fine for now; task 8 fills the logic).
  - Sign out the external cookie: `await HttpContext.SignOutAsync("External");`.
  - Call `ITokenService.HandlePlayerAuth(user)` to mint session + set auth cookies.
  - Redirect to `returnUrl` (validate it's same-origin first — never redirect to an arbitrary URL).
- `fasternfaster.api/Program.cs`:
  - Confirm cookie auth handler named `"External"` is registered and acts as `SignInScheme` for the Google handler. Current setup may only have JWT + Google; add `.AddCookie("External")` between them.
  - Set `GoogleOptions.SignInScheme = "External"` if not already.
- **Frontend**:
  - `fasternfasterapp/src/features/auth/hooks/useGoogleLogin.js` — `window.location.href = '/api/auth/google?returnUrl=' + encodeURIComponent(window.location.origin + '/auth/callback')`.
  - `fasternfasterapp/src/pages/AuthCallback/AuthCallback.js` — current stub logs the code; change to: bootstrap session from cookies already set by backend, then `navigate('/lobbies')`. If no session detected, navigate to `/` with an error query param.

## Approach

- Unified endpoint = single URL for both login and register; the find-or-create logic in task 8 makes the distinction invisible to the user.
- Backend sets auth cookies during the callback redirect — so by the time `AuthCallback.js` loads, session is already active. No token-in-URL shenanigans.
- `returnUrl` validation: parse with `Uri`, compare `Host` to allowed origins from `appsettings.json`.

## Acceptance

- Clicking "Sign in with Google" in the UI goes through Google's consent screen.
- Google redirects to `/api/auth/google/callback`, which issues session cookies and redirects to `/auth/callback` on the frontend, which lands the user on `/lobbies`.
- Tested against a real dev ClientId/Secret.
- The old `/api/auth/register/google` route is gone.

## Risks / notes

- Google's handler is OpenID Connect — need to ensure the scope includes `openid email profile` so `email` is in the claims.
- If `Program.cs` currently uses `.AddGoogleOpenIdConnect()` — confirm it still signs into a cookie scheme. If it signs into JWT directly, this task gets messier.
- Do NOT rely on the cookie that Google sets post-callback being the app's JWT. The flow is: Google → External cookie (transient) → our callback exchanges that for our JWT.
