# CLAUDE.md

- Write clean, testable code following SOLID, YAGNI, DRY, and KISS principles
- Cover new code with tests
- If something is unclear before implementing, ask first

## Code style

Single convention, backend and frontend. Check a neighbouring file before adding anything new; if it conflicts with a rule below, the rule below wins — it exists because the codebase had drifted.

- **Private fields (C#):** plain `camelCase`, no leading underscore (`logger`, `budget`, not `_logger`/`_budget`). This is the dominant existing style; a few older files use `_camelCase` for an injected clock — don't copy that, and feel free to fix one in passing if you're already touching that file.
- **Comments:** default to none. Add one only when the code can't explain itself without it — a non-obvious business rule, a reason a design choice must stay a certain way, a trick in a test. Never add a comment that just restates a well-named method or the next line. No section-banner comments (`// -------- happy path --------`).
- **Namespaces:** file-scoped (`namespace X;`), not block-scoped. Already consistent; keep it that way.
- **Blank lines:** single blank line between members at most; no double blank lines.
- **Tests:** `Method_Scenario_Expected` naming. Extract a named helper for a repeated multi-step setup instead of commenting each call site.
- **`.editorconfig`** enforces the private-field rule and basic formatting; let your editor/IDE flag violations rather than relying on memory.
