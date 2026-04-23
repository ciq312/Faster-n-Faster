---
name: Authorization
description: Consolidate authorization — finalize stateful JWT sessions, wire up Google OAuth, and add email + verification
---

# Authorization

## Summary

Consolidate and finalize the authorization story for the app. We already have stateful JWT auth backed by `SessionService` (sessions live server-side because this is a game, not a stateless REST app), an anonymous-guest flow, and classic email/password registration. Google OAuth endpoints are scaffolded but not wired up end-to-end. This feature completes the Google sign-in flow (find-or-create the user on callback), adds a proper `email` column to the user entity, and introduces email verification so that a registered account has a provable contact address. The end state: a user can sign up with email/password OR Google, gets an email verification step, and lands in a server-issued JWT session identical to what already exists.

## Goals

- Finalize the Google OAuth login/register endpoint: on successful callback, look up the user by Google subject (via `ExternalLogins` table) or email, and either issue a session for the existing user or create a new user row, then issue a session.
- Add an `Email` field to the `User` entity and persist it for both email/password and Google registrations.
- Add an `EmailVerified` flag on users and an email-verification flow (send token, verify via link).
- Add an `ExternalLogins` table (`userId`, `provider`, `externalSubject`, `externalEmail`) so future providers (Discord, GitHub, etc.) can be added without schema changes.
- Keep stateful JWT + `SessionService` as the single source of truth for "who is logged in" — OAuth and email/password both terminate in the same session mechanism.
- Drop unique-nickname constraint (nicknames no longer need to be unique).

## Non-Goals

- Switching away from stateful sessions to pure stateless JWT.
- Additional OAuth providers beyond Google (GitHub, Discord, etc.).
- Password reset flow (likely a follow-up, but not part of this spec unless we decide to fold it in — see open question 5).
- 2FA / MFA.
- Role-based authorization / admin accounts.
- Rate-limiting and abuse protection for the verification email endpoint (should be considered but out of scope for the spec itself).

## User Experience

**Email/password registration (existing, extended):**

1. User fills out registration form — now includes an `email` field alongside username/password.
2. Server creates the user with `EmailVerified = false`, issues a session, sends a verification email with a tokenized link.
3. User can use the app normally, but a banner/indicator shows "email not verified" (see open question 2 — do we gate any features on verification?).
4. User clicks the link, backend verifies the token, sets `EmailVerified = true`, redirects to the app.

**Google sign-in (new, end-to-end):**

1. User clicks "Sign in with Google" on the registration/login page.
2. Frontend redirects to `/api/auth/register/google` (or a unified `/api/auth/google`) with a `returnUrl`.
3. Backend redirects to Google's OAuth consent screen.
4. Google redirects back to our callback endpoint with an authorization code.
5. Backend exchanges code for Google profile info (sub, email, name).
6. Backend runs "find-or-create":
   - Look up `ExternalLogins` by (`provider = "google"`, `externalSubject = sub`) → if found, load the linked user.
   - Otherwise look up `User` by `Email` → if found, auto-link by inserting a new `ExternalLogins` row and load the user.
   - Otherwise create a new `User` (with the Google email, `EmailVerified = true`) and a matching `ExternalLogins` row.
7. Backend issues a session via `SessionService` and redirects to `returnUrl`.

**Guest → registered upgrade (existing flow, clarified):**

- A guest player has a client-side UUID. When they register (email/password or Google), the server should associate that guest's activity/identity with the new user row if possible (open question 3).

## Edge Cases

- **Google account has no email** (rare but possible for some workspace setups) — reject or prompt for email?
- **Email collision**: user previously registered with `alice@x.com` via password, now signs in with Google using the same email. Do we auto-link the accounts, refuse, or prompt?
search for email in db and if match then just give the user 
- **Unverified email collision**: someone registers with `bob@x.com` and never verifies. Someone else tries to register with the same email. Who wins? the verification I thought should be like this user tries to register then next step is to verify email once it verified the user only then is navigated to the lobbies and gets his token 
- **Verification link expired or already used** — clear error + "resend verification" option. resent message option
- **User changes email** — does that reset `EmailVerified`? (Assume yes — see open question 4.)
- **Session already active, user hits `/login` or `/register/google`** — do we no-op, re-auth, or allow account switching? allow account switching
- **Email delivery failure** — user registered but never got the email. Need a "resend" endpoint. true
- **Verification token reuse** — tokens must be single-use and time-limited. true
- **OAuth state/PKCE** — CSRF protection on the OAuth flow must be in place.
- **Existing users (already in DB without email)** — migration: do we backfill `Email` as nullable, or force re-registration? (Likely nullable for now.) i will delete them later
- **Username conflict on Google signup** — Google gives us a display name but our app might require a unique username. Do we auto-generate, or prompt the user? we probably should remove name duplicating and make nicknames not unique at all 

## Open Questions

1. **Account linking policy**: if a user signs in with Google using an email that already exists as a password-registered account, do we (a) auto-link, (b) reject and tell them to log in with password first, or (c) prompt an explicit link step? a
2. **Is any functionality gated on email verification** (e.g., can't create private lobby, can't appear on leaderboard), or is it purely informational in v1? player can't get his access token so he can't play when he's not auth and not a guest
3. **Guest-to-registered migration**: when an anonymous guest registers, do we carry over their `localStorage` UUID / past race stats onto the new user record, or is the guest identity dropped entirely? dropped entirely for now 
4. **Email change flow**: is changing email in scope for this spec? If yes, does it re-trigger verification and, until verified, does the old email still count?  out of the scope of this spec 
5. **Password reset**: fold into this spec or defer? (Verification infra overlaps heavily — single-use tokenized emails.)
we want to make the password reset
6. **Token delivery mechanism**: what email provider are we using (SendGrid, Mailgun, SMTP via Gmail, dev-only console logging)? Decision affects config, env vars, and local dev experience. SMTP via email probably I don't know suggest what's best
7. **Verification token lifetime**: 24h? 1h? Configurable? 30 min
8. **What does the Google endpoint URL look like in the final design** — keep `/api/auth/register/google` + a separate `/api/auth/login/google`, or unify into one `/api/auth/google` that does find-or-create? (Unified is simpler.) let's make unified
9. **Username requirement for Google users**: Google gives us a display name but the app may need a unique username. Auto-generate from the Google name with a disambiguator, or prompt on first login? I said we probably don't want it to be unique anymore
10. **Storage of Google identity**: resolved — use a separate `ExternalLogins` table (`userId`, `provider`, `externalSubject`, `externalEmail`). Adding Discord/GitHub later = new rows, no schema change.

## Notes

- Stateful JWT means the JWT carries a session id and `SessionService` is authoritative for validity — logout, revocation, and "kick from lobby" remain easy to implement. This must not change.
- `userId` in player records is already nullable per project convention, which is what makes guest → registered upgrade viable.
- `GoogleRegisterUserEndpoint.cs` exists but currently does nothing in `HandleAsync` beyond constructing `AuthenticationProperties`. That's the main thing this spec asks to finish.
- Existing `RegisterUserValidator` / `RegisterUserRequest` will need an `email` field. The validator should enforce format.
- Verification emails are a new external-dependency surface area (SMTP or a provider SDK). That's a meaningful environmental change — coordinate with `appsettings.json` / env config.
