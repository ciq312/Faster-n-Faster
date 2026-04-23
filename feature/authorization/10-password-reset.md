# Task 10 — Password reset

## Goal

User forgets password → requests reset via email → clicks a tokenized link → sets a new password. All existing sessions for that user are invalidated on reset.

## Files to touch

- `fasternfaster.api/Core/Entities/EmailVerificationToken.cs` (from task 5) — extend with a `Purpose` enum column: `Verification | PasswordReset`. Rename the entity to `AuthToken` (single table, two use cases) or keep the current name — **recommend rename to `AuthToken`** for clarity.
- Migration: add `Purpose` column; default existing rows to `Verification`.
- `VerificationTokenService` → rename to `AuthTokenService` with methods `GenerateVerificationAsync(User)` and `GeneratePasswordResetAsync(User)` — both create tokens but with different `Purpose` and potentially different TTLs (reset = 30 min, same as verification, per Q7).
- **NEW** use case `fasternfaster.api/UseCases/Users/RequestPasswordReset/`:
  - `RequestPasswordResetCommand` (email).
  - Handler: find user by email; if found, generate `PasswordReset` token + send mail via `IEmailSender`. Always return 200 (no enumeration).
- **NEW** use case `fasternfaster.api/UseCases/Users/ResetPassword/`:
  - `ResetPasswordCommand` (raw token, new password).
  - Handler: validate token (unused, unexpired, `Purpose == PasswordReset`), update `User.Password` (hashed), mark token used, `sessionService.InvalidateAll(user.Id)`.
- **NEW** endpoints:
  - `Web/Users/RequestPasswordReset/RequestPasswordResetEndpoint.cs` — POST `/api/auth/forgot-password` `{email}`.
  - `Web/Users/ResetPassword/ResetPasswordEndpoint.cs` — POST `/api/auth/reset-password` `{token, newPassword}`.
- Validators: `ResetPasswordValidator` — same password rules as registration.
- **Frontend**:
  - **NEW** `src/pages/ForgotPassword/ForgotPassword.js` — email input → calls `/api/auth/forgot-password` → shows "check your inbox".
  - **NEW** `src/pages/ResetPassword/ResetPassword.js` — reads `?token=` from URL, new-password form, submits to `/api/auth/reset-password`, navigates to `/login` on success.
  - Add a "Forgot password?" link on the login form (`SignupForm.js` / login component).
  - Routes `/forgot-password` and `/reset-password` in `App.js`.

## Approach

- Reuse the token infra from task 5. Adding a `Purpose` column is cheaper than a parallel table and keeps expiry/hashing logic in one place.
- `forgot-password` endpoint **always** returns 200 regardless of whether the email exists — prevents account enumeration.
- Reset **invalidates all sessions** for the user — critical security property. A leaked token in old sessions must not remain valid after a password change.
- Google-only users (no password set): sending a reset email is ambiguous — either reject with a clear "this account uses Google sign-in" message, or silently no-op. **Recommend: silent no-op** (still return 200) to match the no-enumeration rule.

## Acceptance

- Request reset with a registered email → mail arrives in Papercut → clicking link opens reset page.
- Submitting new password → old password no longer works; new password logs in.
- Old refresh tokens for that user are rejected after reset.
- Unknown email → 200 with no side effects.
- Reused or expired reset token → 400.
- Google-only user requesting reset → 200, no email sent (verify in Papercut).

## Risks / notes

- If task 5's token entity is still called `EmailVerificationToken` at the time this task starts, decide whether to rename (one more migration) or accept a slightly-off name. Rename is cleaner long-term.
- Rate-limiting the forgot-password endpoint is worth considering post-MVP (spec marks it out of scope for the spec itself).
