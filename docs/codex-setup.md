# Codex repository setup

`AGENTS.md` is the shared source for project instructions. `CLAUDE.md` imports it with `@AGENTS.md`, so Claude and Codex use the same conventions. Codex-specific workflow instructions live in `.agents/skills/spec/SKILL.md`; the five specialist roles live in `.codex/agents/`. Each role inherits the selected parent model and reasoning effort. Global model, MCP and plugin settings are unchanged.

## Activate

1. Restart Codex in this trusted repository to discover the new instructions, skill and agents.
2. Open `/hooks` in the CLI, or **Settings > Hooks** in the desktop app. Review and trust the repository hooks. Changed definitions/scripts may require another review.
3. Ask to use `$spec` for a ticket, or request a named role such as `code-reviewer`. The workflow requires plan approval before implementation and authorization before commit/PR actions; existing explicit approvals count.

The project config enables hooks and agents, with at most three concurrent spawned agents. It does not grant broader filesystem, network or Jira permissions. Custom roles request read-only behaviour where appropriate; they inherit the parent's sandbox and MCP access, so this is not an independent permission boundary.

## Guards

`dispatch.py` normalizes Codex shell arguments and delegates to adapted copies of the Claude branch and docs guards. The branch guard blocks tracked edits on protected branches, validates feature branch naming/base and checks cached remote refs. Apply-patch calls are conservatively checked against the session repository. The docs guard flags commits whose changed files are referenced by unstaged documentation; after reviewing the indicated passages, the same unchanged diff may be committed again, matching the Claude workflow.

Explicit real `.env`/`secrets/` paths and `curl` commands are blocked by the dispatcher; `.env.example` remains usable. These hooks are convenience guards, not exhaustive shell analysis or filesystem read protection. Indirect commands and specialized tool paths may evade matching. Keep sandbox and permission controls enabled and follow AGENTS.md even for tools outside hook coverage. MCP mutations still require authorization from the user or an explicitly invoked workflow.

The adapters are self-contained, so ignored local `.claude/hooks/` files are not required on another checkout. Updating the Claude guards does not automatically update these copies; compare them when policy changes. No plugin credentials, personal settings or Claude history were copied.

## Verify

```bash
python3 .codex/hooks/test_guards.py
```

This exercises protected-branch edits, patch handling, permitted feature edits, branch naming/base checks, explicit secret paths, shell normalization and the docs-review checkpoint in disposable repositories. Live hook activation still requires the host's trust review.

References: [Codex hooks](https://learn.chatgpt.com/docs/hooks), [custom agents](https://learn.chatgpt.com/docs/agent-configuration/subagents), [project instructions](https://learn.chatgpt.com/docs/agent-configuration/agents-md).
