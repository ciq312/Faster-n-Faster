# Task 06 — Gate sessions on email verification

## Goal

No JWT / refresh token is issued until `EmailVerified = true`. Registration returns 201 without a token; login returns 403 if the user isn't verified yet.

## Files to touch

- `fasternfaster.api/Web/Services/Implementations/{TokenService}.cs` (wherever `ITokenService.HandlePlayerAuth` lives) — branch early: if `user.EmailVerified == false`, return a distinguished result (e.g., `AuthResult.EmailNotVerified(user.Email)`).
- `fasternfaster.api/Web/Users/LoginUser/EndPoints/LoginUserEndpoint.cs` — map the new result → 403 with body `{ code: "EMAIL_NOT_VERIFIED", email }`.
- `fasternfaster.api/UseCases/Users/RegisterUsers/Handlers/RegisterUserHandler.cs` — after creating the user + sending the verification email, **do NOT call `HandlePlayerAuth`**. Return `{ emailSent: true, email }`.
- `fasternfaster.api/Web/Users/RegisterUser/EndPoints/RegisterUserEndpoint.cs` — return 201 with the handler's result; no Set-Cookie for auth cookies.
- **Frontend**:
  - `fasternfasterapp/src/features/auth/hooks/useRegister.js` — on 201 success, navigate to `/check-your-email?email=...` instead of auto-logging in.
  - `fasternfasterapp/src/features/auth/hooks/useLogin.js` — on 403 with `EMAIL_NOT_VERIFIED`, navigate to `/check-your-email?email=...`.
  - **NEW** `fasternfasterapp/src/pages/CheckYourEmail/CheckYourEmail.js` — show the email address, a "resend link" button calling `/api/auth/resend-verification`, and a "wrong address? register again" link.
  - Add route `/check-your-email` in `App.js`.

## Approach

- Google users are not affected: `ExternalLoginService.FindOrCreateAsync` creates them with `EmailVerified = true` (task 8), so the gate doesn't fire.
- 403 (not 401) because the user's credentials were correct — they just haven't completed onboarding. 401 implies "bad credentials" and would confuse the frontend's auto-refresh logic in `apiCall.js`.

## Acceptance

- Registering a new account returns 201 and does NOT set auth cookies / return a JWT.
- Attempting to log in with correct credentials before verification returns 403 `EMAIL_NOT_VERIFIED`.
- After clicking the verification link, logging in with the same credentials works normally.
- `/check-your-email` page shows the email and a functional resend button.

## Risks / notes

- Guest auth (`RegisterAnonymous`) must stay untouched — guests don't have emails.
- Make sure `apiCall.js` does not treat 403 the same as 401 (the 401 path auto-refreshes; 403 should surface to the caller).
