# 0010. Business errors as exception categories, mapped to HTTP in Presentation

- **Status:** Accepted
- **Date:** 2026-06-26 (status exceptions), 2026-10-13 (mapping moved to Presentation)
- **Sources:** [58bd647](https://github.com/ciq312/Faster-n-Faster/commit/58bd647) (status exception middleware), [99d2ab1](https://github.com/ciq312/Faster-n-Faster/commit/99d2ab1) (remove try/catch from endpoints in favor of exception filters), [7e18d7f](https://github.com/ciq312/Faster-n-Faster/commit/7e18d7f) (replace DomainException with status exceptions), [#27](https://github.com/ciq312/Faster-n-Faster/pull/27) (surface business-rule violations to players), [#33](https://github.com/ciq312/Faster-n-Faster/pull/33) (email-not-verified redirect), [#41](https://github.com/ciq312/Faster-n-Faster/pull/41) (map status codes at the Web layer)

## Context

Business rules fail all the time (lobby full, color taken, not the host, wrong password), and each failure has to reach the player as a clear message, with a sensible status code over HTTP. Endpoints used to catch exceptions themselves. Then the exceptions carried their own status number (`StatusException.StatusCode`, `BadRequestException` = 400), which put HTTP into the domain, a layer that should have no outside dependencies ([0003](0003-clean-architecture-over-layered.md)).

## Decision

A business error is an exception class that derives from one of five categories, and one handler per transport maps categories to the wire. Neither the domain nor the use cases know about HTTP.

```
StatusException (Domain: message, optional Code, optional Extensions)
├── BadRequestException     ← HostKickSelfException, HostTransferToSelfException, …
├── NotFoundException       ← UserNotFoundException, InvalidInviteCodeException, …
├── ConflictException       ← AlreadyInLobbyException, ColorNotInPaletteException, …
├── ForbiddenException      ← NotHostException, EmailNotVerifiedException, …
└── UnauthorizedException   ← InvalidCredentialsException
```

- **HTTP:** `StatusExceptionHandler` (an ASP.NET `IExceptionHandler` in `Presentation/Web/Shared`) maps the category to 400/404/409/403/401 with one `switch`. It writes `{ "error": message }`, plus `code` and any extension fields.
- **Hub:** `HubExceptionFilter`, a SignalR hub filter, turns any `StatusException` into a `HubException` carrying the message.
- A `code` is added only when a client branches on it (today only `EMAIL_NOT_VERIFIED`).

## Why: the open-closed principle

A new business rule needs a new error. Adding one is a new file, like the existing `LobbyFullException`:

```csharp
public class LobbyFullException : ConflictException
{
    public LobbyFullException() : base("Lobby is full") { }
}
```

The code that throws it changes, as it must. Nothing else does: `StatusExceptionHandler`, `HubExceptionFilter`, the endpoints and the hub stay as they are. Today 20 exceptions go through a 5-arm `switch`. The mapping is closed for modification and open for extension.

The boundary is a new category, meaning a status code no category maps to yet. That is one new class and one new `switch` arm, and it's rare.

The second reason is the dependency direction: the domain says what kind of failure happened (conflict, not found), and Presentation decides what that means in HTTP. The domain project has no HTTP, persistence or serialization attributes.

The full error contract for clients is in the README's API section.

## Alternatives considered

- **Try/catch in each endpoint.** Where it started. Every endpoint repeated the same mapping, and adding an error meant editing them.
- **Status codes on the exceptions.** Removed for the dependency reason above.
- **Result types instead of exceptions.** Not recorded as considered.

## Consequences

- One mapping per transport, and the domain stays free of HTTP.
- The category names still mirror HTTP's vocabulary (`BadRequest`, `Forbidden`), so the domain isn't protocol-neutral in its naming.
- Clients branch only on `code`, never on message text.
