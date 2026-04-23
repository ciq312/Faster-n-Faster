# Task 05 — Email verification flow

## Goal

After registration, user receives a tokenized link. Clicking it flips `EmailVerified = true`. Tokens are single-use and expire in 30 minutes. Resend is supported.

## Files to touch

- **NEW** `fasternfaster.api/Core/Entities/EmailVerificationToken.cs`: `Id`, `UserId`, `TokenHash` (SHA-256 of the raw token — store hash, never the raw value), `ExpiresAt`, `UsedAt` (nullable), `CreatedAt`.
- `fasternfaster.api/infrastructure/AppDbContext.cs` — add `DbSet<EmailVerificationToken>`; unique index on `TokenHash`; FK to `User`.
- New migration.
- **NEW** use case `fasternfaster.api/UseCases/Users/VerifyEmail/`:
  - `VerifyEmailCommand` (raw token string).
  - `VerifyEmailHandler`: hash the token, look it up, validate `UsedAt == null` and `ExpiresAt > now`, set `UsedAt`, flip `User.EmailVerified = true`. Throw `InvalidTokenException` otherwise.
- **NEW** use case `fasternfaster.api/UseCases/Users/ResendVerification/`:
  - `ResendVerificationCommand` (email).
  - Handler: find user by email; if verified → no-op (return success); else invalidate existing unused tokens, generate a new one, send email. Always return 200 (no enumeration leak).
- **NEW** endpoints:
  - `Web/Users/VerifyEmail/VerifyEmailEndpoint.cs` — GET `/api/auth/verify-email?token=...`.
  - `Web/Users/ResendVerification/ResendVerificationEndpoint.cs` — POST `/api/auth/resend-verification` with `{email}`.
- `UseCases/Users/RegisterUsers/Handlers/RegisterUserHandler.cs` — after user is created, generate token + send verification email via `IEmailSender`.
- Token generation helper: `fasternfaster.api/Web/Services/Implementations/VerificationTokenService.cs` — `GenerateAsync(User)` returns `(rawToken, entity)`; `HashAsync(raw)` returns SHA-256 hex.
- **Frontend**:
  - **NEW** `fasternfasterapp/src/pages/VerifyEmail/VerifyEmail.js` — reads `?token=` from URL, POSTs to `/api/auth/verify-email`, shows success/error states, offers "request new link" on error.
  - Route it at `/verify-email` in `App.js`.

## Approach

- **Raw token format**: 32 bytes from `RandomNumberGenerator.GetBytes(32)`, Base64URL-encoded → ~43 chars, URL-safe.
- Link shape: `{frontendBaseUrl}/verify-email?token={raw}` — frontend page calls the backend. Keeps Accept/Content-Type clean (JSON).
- Store only the SHA-256 hash of the token. Raw token is sent once in the email and never persisted server-side.
- On resend: invalidate all unused tokens for that user (set `UsedAt = now`) before generating a fresh one — prevents ballooning the table.

## Acceptance

- Register → mail arrives in Papercut → clicking link flips DB `EmailVerified = true`.
- Expired token (>30 min) → 400 with clear error.
- Reusing a used token → 400.
- Resending works and superseeds old tokens.
- POST `/api/auth/resend-verification` with unknown email returns 200 (no enumeration).

## Risks / notes

- Purpose of hashing: if the DB is ever leaked, attackers can't verify emails they don't own.
- Frontend base URL must be config-driven (add `App:FrontendBaseUrl` in `appsettings.json`) so verification links work in dev vs. prod.
