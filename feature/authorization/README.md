# Authorization — Task Plans

Per-task plans for the authorization feature. Start with `spec.md` for context; then pick up tasks in order (most later tasks depend on earlier ones).

## Order

1. [Schema migration](01-schema-migration.md) — add `Email`, `EmailVerified`, `ExternalLogins` table. **Blocks everything else.**
2. [Remove nickname uniqueness](02-remove-nickname-uniqueness.md) — drop duplicate-nick enforcement.
3. [Email on register](03-email-on-register.md) — accept + persist email in email/password registration.
4. [SMTP infrastructure](04-smtp-infrastructure.md) — `IEmailSender` + MailKit SMTP impl.
5. [Email verification flow](05-email-verification-flow.md) — token entity, verify + resend endpoints. Depends on 3 + 4.
6. [Gate sessions on verification](06-gate-sessions-on-verification.md) — no JWT until verified. Depends on 5.
7. [Google OAuth endpoint](07-google-oauth-endpoint.md) — unified `/api/auth/google`.
8. [Google find-or-create](08-google-find-or-create.md) — user lookup via `ExternalLogins`. Depends on 1 + 7.
9. [Account switching](09-account-switching.md) — invalidate old session on new login.
10. [Password reset](10-password-reset.md) — forgot + reset endpoints. Reuses token infra from 5.

## Dependencies

- Tasks 2–10 all assume the schema migration (task 1) has shipped.
- Task 5 needs the SMTP infrastructure (task 4) to actually deliver mail.
- Task 6 depends on task 5 (otherwise verification can't be completed and no one can log in).
- Task 8 depends on task 7 for the OAuth plumbing.
- Task 10 reuses the verification-token infrastructure from task 5.
