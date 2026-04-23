# Task 01 — Schema migration

## Goal

Extend the DB schema to support email, email verification, and a provider-agnostic external-login table.

## Files to touch

- `fasternfaster.api/Core/Entities/User.cs` — add `Email` (string, nullable), `EmailVerified` (bool, default `false`). Private setters; add methods `SetEmail(string)` and `MarkEmailVerified()` for controlled mutation.
- `fasternfaster.api/Core/Entities/ExternalLogin.cs` — **NEW** entity: `Id` (Guid), `UserId` (Guid), `Provider` (string, e.g. `"google"`), `ExternalSubject` (string), `ExternalEmail` (string, nullable), `CreatedAt` (DateTime).
- `fasternfaster.api/infrastructure/AppDbContext.cs`:
  - Add `DbSet<ExternalLogin> ExternalLogins`.
  - In `OnModelCreating`: unique index on `(Provider, ExternalSubject)`; FK `ExternalLogin.UserId → User.Id` with cascade delete.
- New EF migration under `fasternfaster.api/infrastructure/Migrations/`.

## Approach

- `Email` is nullable so existing rows (if any) don't break the migration. User said they'll delete old rows manually — no data backfill needed.
- Don't add unique index on `Login` or `Nick` (nickname uniqueness is dropped in task 2; `Login` uniqueness stays handler-enforced for now — can be tightened later).
- Keep `Provider` as a plain string column (not an enum) to avoid a second migration when Discord/GitHub are added.

## Acceptance

- `dotnet ef migrations add AddEmailAndExternalLogins` generates a migration that compiles.
- `dotnet ef database update` succeeds on a fresh DB.
- `ExternalLogins` table exists with unique index on `(Provider, ExternalSubject)` and FK to `Users`.
- `User` has `Email` (nullable) and `EmailVerified` (bool, default false) columns.

## Risks / notes

- If the project uses snake_case column naming, confirm the migration output matches the existing convention before committing.
- Cascade delete on `ExternalLogin → User` is fine since external logins are meaningless without their parent user.
