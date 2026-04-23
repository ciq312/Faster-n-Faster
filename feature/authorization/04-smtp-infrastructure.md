# Task 04 — SMTP infrastructure

## Goal

Application can send transactional email via SMTP. Dev uses Papercut (local capture), prod will use a real SMTP provider (SendGrid recommended); same code path.

## Files to touch

- **NEW** `fasternfaster.api/Web/Services/IEmailSender.cs`:
  ```
  Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct);
  ```
- **NEW** `fasternfaster.api/Web/Services/Implementations/SmtpEmailSender.cs` — uses MailKit (`MailKit` + `MimeKit` NuGet packages). Reads config from `Smtp` section.
- `fasternfaster.api/FasterNFaster.Api.csproj` — add `MailKit` package reference.
- `fasternfaster.api/appsettings.json` — add:
  ```json
  "Smtp": {
    "Host": "localhost",
    "Port": 25,
    "Username": "",
    "Password": "",
    "FromAddress": "noreply@fasternfaster.local",
    "FromName": "Faster'n'Faster",
    "UseStartTls": false
  }
  ```
- `fasternfaster.api/Program.cs`:
  - Bind `Smtp` section to an `SmtpOptions` class.
  - `services.AddScoped<IEmailSender, SmtpEmailSender>();`

## Approach

- MailKit over `System.Net.Mail.SmtpClient` — Microsoft officially recommends MailKit; `SmtpClient` has quirks with modern TLS.
- Dev setup: install **Papercut SMTP** on Windows (listens on `localhost:25`, captures mail in a UI, no DNS). Document this in README.
- Prod setup (later): flip `Smtp.Host` to `smtp.sendgrid.net`, `Port` to `587`, `UseStartTls` true, creds via env var override.
- Keep `SmtpEmailSender` dependency-free beyond MailKit/Options — no coupling to verification logic.

## Acceptance

- Sending a test mail via `IEmailSender.SendAsync("me@local", "hi", "<p>test</p>", ct)` delivers to Papercut and appears in its UI.
- Swapping `Smtp.Host` / `Smtp.Port` in `appsettings.Development.json` does not require code changes.

## Risks / notes

- Do **not** commit real SMTP credentials. Prod creds go through env-var overrides or a secret manager, per project convention.
- MailKit's `SmtpClient` should be instantiated per-send, not held as a singleton — it's not thread-safe and is cheap to create.
