# Style guide for review

One convention, backend (C#) and frontend (React). Check a neighbouring file before flagging something as wrong — the codebase has drifted in places, and these rules win over an older file's habit.

- **Private fields (C#):** plain `camelCase`, no leading underscore (`logger`, `budget`, not `_logger`/`_budget`). A few older files use `_camelCase` for an injected clock; don't ask for that pattern, and a fix-in-passing on a file already being touched is fine.
- **Comments:** default to none. Flag a comment only if it just restates a well-named method or the next line, or if it's a section banner (`// -------- happy path --------`). A comment explaining a non-obvious business rule or a reason a design choice must stay a certain way is wanted, not a smell.
- **Namespaces:** file-scoped (`namespace X;`), not block-scoped.
- **Blank lines:** at most one blank line between members.
- **Tests:** `Scenario_Expected` naming. Prefer a named helper for a repeated multi-step setup over per-line comments explaining each step.
- **Method chains:** leading dot, one call per line once the chain doesn't fit on one line.
- **Formatting:** `dotnet format` and Prettier/ESLint already run in CI, so don't raise pure formatting nits that those tools would catch or fix.

Beyond style, prioritize:

- Correctness bugs and missed edge cases over style nitpicks.
- Reuse of existing classes/patterns in this codebase over new abstractions, following YAGNI/KISS/DRY/SOLID.
- Whether new code has test coverage.

This file mirrors the project's `CLAUDE.md`. If the two ever disagree, `CLAUDE.md` is the source of truth.
