---
name: spec
description: Run a Faster-n-Faster Jira ticket through briefing, research, planning, implementation, review and PR preparation. Use when asked to start or pick up a SCRUM ticket or run the delivery workflow.
---

# Ticket delivery

Read repository AGENTS.md. Delegate the roles below using the project custom agents. If the client cannot select custom roles, read the corresponding `.codex/agents/<role>.toml` and pass its instructions explicitly to a supported subagent. Keep specialist Jira access read-only; the parent performs authorized lifecycle changes. Agent model and effort inherit the parent.

1. Delegate `jira-task-briefer` for the given key or next unfinished task. Present the brief. For operations-only work, provide a concrete runbook unless the user has asked you to execute it.
2. Delegate `feature-researcher` if there are materially different approaches. Check related tickets, recent dev changes and current code so redundant work stops with evidence.
3. Delegate `implementation-planner`, including the brief and chosen approach. Resume the same planner with answers to material questions. Present approach, decisions, affected files and verification. Wait for plan approval unless the conversation already authorizes this plan.
4. Prepare a feature branch from current dev before implementation. Preserve existing changes. Reuse an appropriate existing feature branch for the same task; do not discard or silently stash unrelated work. Raise conflicts in scope or ownership when necessary.
5. Delegate `senior-engineer` with the approved plan. Inspect the actual diff and validation results yourself.
6. Delegate `code-reviewer`. Fix verified must-fix findings and repeat focused review when needed. Explain optional findings consciously deferred.
7. Present changed files, deviations, review outcome, actual checks and proposed Conventional Commit title. Wait for authorization to commit/push/open a PR unless already given. This approval is separate from implementation approval.
8. When authorized, create title-only Conventional Commits and a title-only PR into dev, without ticket keys or attribution trailers. Do not merge or enable auto-merge. Only when the user authorized the Jira lifecycle change, discover the appropriate transition and move the ticket to In Review after opening the PR.
9. Inspect CI, fix failures introduced by this change and report the outcome. Move the ticket to Done only after verifying the PR merged and when that Jira action is authorized. Otherwise leave it In Review.

Role details belong to `.codex/agents/`; coding and command conventions belong to AGENTS.md. Follow explicit user scope and existing approvals; do not add ownership confirmation once implementation was requested.
