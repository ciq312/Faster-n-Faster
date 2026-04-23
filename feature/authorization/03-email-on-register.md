# Task 03 — Email on registration

## Goal

Accept and persist an email address on email/password registration. Reject duplicates and malformed addresses.

## Files to touch

- `fasternfaster.api/UseCases/Users/RegisterUsers/Commands/RegisterUserCommand.cs` — add `Email` property.
- `fasternfaster.api/UseCases/Users/RegisterUsers/Handlers/RegisterUserHandler.cs`:
  - Add duplicate-email check (via `IUserRepository.GetByEmailAsync` or equivalent).
  - On create, set `Email` and leave `EmailVerified = false` (default).
  - Throw a new `DuplicateEmailException` on collision.
- `fasternfaster.api/Web/Users/RegisterUser/RegisterUserRequest.cs` — add `Email`.
- `fasternfaster.api/Web/Users/RegisterUser/RegisterUserValidator.cs` — `RuleFor(x => x.Email).NotEmpty().EmailAddress()`.
- `fasternfaster.api/Web/Users/RegisterUser/EndPoints/RegisterUserEndpoint.cs` — catch `DuplicateEmailException`, return 409.
- Frontend:
  - `fasternfasterapp/src/pages/Registration/SignupForm.js` — add Email input, basic client-side email regex.
  - `fasternfasterapp/src/features/auth/hooks/useRegister.js` — include `email` in the request body; handle 409 "email already registered".

## Approach

Standard request-plumbing task. No token / email-sending changes yet — that arrives in task 5.

## Acceptance

- POST `/api/auth/register` with `{login, nick, password, email}` persists all four; `Email` appears in the `users` row, `EmailVerified = false`.
- Duplicate email → 409.
- Missing/malformed email → 400 from validator.
- UI shows friendly error messages for both.

## Risks / notes

- Don't normalize email casing at write time — either normalize both on write and read, or leave as-is. Prefer **normalize-on-write** (lowercase) so duplicate checks are case-insensitive without needing `ILIKE`.
