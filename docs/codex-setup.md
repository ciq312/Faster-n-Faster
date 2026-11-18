# Codex repository setup

`AGENTS.md` is the canonical source for repository instructions used by Codex. `CLAUDE.md` contains `@AGENTS.md`, so Claude Code imports the same instructions. The Jira delivery workflow lives in `.agents/skills/spec/SKILL.md`; the five custom roles live in `.codex/agents/`. The roles inherit the active parent model, reasoning effort, sandbox and connected tools unless explicitly configured otherwise. These repository files do not configure personal/global model, MCP or plugin settings.

## Activate

1. Start a new Codex session in this repository to load its instructions, skill and custom roles.
2. Review and trust the repository hook in `/hooks` (CLI) or **Settings > Hooks** (desktop). The hook definition invokes `.codex/hooks/dispatch.py`; that script invokes the two guard scripts. Hook activation depends on host trust, and script/definition changes may require review again.
3. Ask to use `$spec` for a Jira ticket, or request a custom role such as `code-reviewer`. The skill describes approval checkpoints; existing explicit user authorization still applies.

`.codex/config.toml` enables hooks and custom agents and sets the session limit to three concurrent agent threads. This is repository configuration; it does not itself grant filesystem, network or Jira access. Custom roles inherit the parent's sandbox and connected tools, so role instructions are not an independent security boundary.

## Guards

`dispatch.py` normalizes tool arguments, blocks explicit real `.env`/`secrets/` paths and `curl` shell commands, then runs both guards. The branch guard rejects Edit/Write operations on non-ignored files in this repo's protected branches, rejects `apply_patch` on those branches, and blocks commits there. When it sees a branch-creation command it checks the branch name and base; while planning it attempts a bounded fetch of the integration branch to report whether local `dev`/`develop` is behind. The docs guard only acts on commit commands. It compares staged changes (and unstaged tracked changes for `commit -a/--all`) to names mentioned in `docs/*.md` and the root `README.md`; a matching doc must be included or explicitly reviewed. For an unchanged diff, it records a one-time review marker so the same commit command can be retried.

The secret and `curl` checks are textual guardrails, not exhaustive shell analysis or filesystem read protection; indirect commands and tool paths that bypass this hook may evade them. Keep sandbox and permission controls enabled and follow `AGENTS.md` for tools outside hook coverage. MCP mutations still require user authorization or an explicitly authorized workflow.

The Codex guards are checked into `.codex/hooks/` and do not depend on the ignored local `.claude/hooks/` scripts. The Claude and Codex hook implementations are separate; update and review both if their shared policy needs to change. No personal credentials or global settings are stored in this repository configuration.

## Verify

```bash
python3 .codex/hooks/test_guards.py
```

This runs the guard unit tests using disposable repositories. They cover protected-branch edits and commits, patch handling, feature edits, branch naming/base checks, explicit secret paths, shell argument normalization and the docs-review checkpoint. It does not verify that the host has activated or trusted the hook; that is checked through `/hooks` or **Settings > Hooks**.

References: [Codex hooks](https://learn.chatgpt.com/docs/hooks), [custom agents](https://learn.chatgpt.com/docs/agent-configuration/subagents), [project instructions](https://learn.chatgpt.com/docs/agent-configuration/agents-md).
