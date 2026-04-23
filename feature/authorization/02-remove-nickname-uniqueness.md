# Task 02 — Remove nickname uniqueness

## Goal

Allow multiple users to share the same nickname (display name). Only `Login` and `Email` remain uniquely constrained at the handler level.

## Files to touch

- `fasternfaster.api/UseCases/Users/RegisterUsers/Handlers/RegisterUserHandler.cs` — remove the duplicate-nick check. Keep the duplicate-login check.
- `fasternfaster.api/Web/Users/RegisterUser/EndPoints/RegisterUserEndpoint.cs` — remove the `catch (DuplicateNickException)` 409 path.
- `DuplicateNickException.cs` — delete the class if nothing else references it (grep first).
- `fasternfasterapp/src/pages/Registration/SignupForm.js` and `fasternfasterapp/src/features/auth/hooks/useRegister.js` — remove any UI handling for a "nick already taken" error.

## Approach

Mechanical cleanup. No DB migration needed (no unique index was ever created on `Nick` — uniqueness was purely handler-enforced).

## Acceptance

- Registering two users in succession with the same `Nick` but different `Login` + `Email` succeeds.
- No `DuplicateNickException` references remain in the codebase.
- Frontend does not surface a "nickname taken" error path.

## Risks / notes

- Lobbies / race results may display multiple "Alex" players. That's acceptable per the feature spec (Q9).
- If lobby UI needs to disambiguate, that's a separate frontend concern — not part of this task.
