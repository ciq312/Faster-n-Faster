#!/usr/bin/env bash
# PreToolUse: keeps work on the branches this repo's flow expects.
# The flow is read from the remote: Gitflow when origin has dev/develop, trunk-based otherwise.
set -uo pipefail

# The spec skill always uses feature/ (the change type lives in the commit title),
# and ticket keys stay out of branch names because the repo is public.
ALLOWED_PREFIXES='^feature/'
TICKET_KEY='scrum-[0-9]+'

input=$(cat)
tool=$(jq -r '.tool_name // empty' <<<"$input")
project=${CODEX_PROJECT_DIR:-$(jq -r '.cwd // empty' <<<"$input")}
git -C "$project" rev-parse --git-dir >/dev/null 2>&1 || exit 0
common_dir=$(git -C "$project" rev-parse --path-format=absolute --git-common-dir)

deny() {
  jq -n --arg r "$1" '{hookSpecificOutput: {hookEventName: "PreToolUse", permissionDecision: "deny", permissionDecisionReason: $r}}'
  exit 0
}

context() {
  jq -n --arg c "$1" '{hookSpecificOutput: {hookEventName: "PreToolUse", additionalContext: $c}}'
  exit 0
}

integration=""
for candidate in dev develop; do
  if git -C "$project" show-ref --verify --quiet "refs/remotes/origin/$candidate"; then
    integration=$candidate
    break
  fi
done
default=$(git -C "$project" symbolic-ref --quiet --short refs/remotes/origin/HEAD 2>/dev/null)
default=${default#origin/}
default=${default:-main}
base_branch=${integration:-$default}

if [ -n "$integration" ]; then
  flow="Gitflow: work goes on feature/<short-meaning> (no ticket key) branched from an up-to-date $integration and returns by a squash PR; $default only takes release PRs from $integration"
  start_cmd="git switch $integration && git pull && git switch -c feature/<short-meaning>"
else
  flow="trunk-based: work goes on a short-lived branch off $default and returns by PR"
  start_cmd="git switch $default && git pull && git switch -c <short-meaning>"
fi

is_protected() { [ "$1" = "$default" ] || [ "$1" = "$integration" ]; }
branch_at() { git -C "$1" symbolic-ref --quiet --short HEAD 2>/dev/null; }

deny_protected() {
  deny "You're on '$1', which this repo's flow keeps protected ($flow). $2 Ask the user to start a branch ($start_cmd), or create it yourself if they've said you may, then retry."
}

planning_context() {
  timeout 10 git -C "$project" fetch --quiet origin "$base_branch" 2>/dev/null
  local behind dirty
  behind=$(git -C "$project" rev-list --count "$base_branch..origin/$base_branch" 2>/dev/null || echo "an unknown number of")
  dirty=$(git -C "$project" status --porcelain | wc -l)
  context "Git state before planning: on '$1'; local $base_branch is $behind commit(s) behind origin/$base_branch; $dirty uncommitted path(s). Flow: $flow. Plan against an up-to-date $base_branch and have the work branch in place before implementing. If $base_branch is behind, or the uncommitted changes belong to other work, tell the user before planning."
}

case "$tool" in
  Edit|Write|NotebookEdit|apply_patch)
    file=$(jq -r '.tool_input.file_path // .tool_input.notebook_path // empty' <<<"$input")
    if [ "$tool" = apply_patch ]; then
      branch=$(branch_at "$project") || exit 0
      is_protected "$branch" && deny_protected "$branch" "Files are not changed directly on it."
      exit 0
    fi
    [ -n "$file" ] || exit 0
    dir=$(dirname "$file")
    while [ ! -d "$dir" ]; do dir=$(dirname "$dir"); done
    # Other repos and other directories are none of this guard's business; worktrees of this repo are.
    [ "$(git -C "$dir" rev-parse --path-format=absolute --git-common-dir 2>/dev/null)" = "$common_dir" ] || exit 0
    git -C "$dir" check-ignore -q "$file" && exit 0
    branch=$(branch_at "$dir") || exit 0
    is_protected "$branch" && deny_protected "$branch" "Files aren't changed directly on it."
    ;;

  Bash)
    cmd=$(jq -r '.tool_input.command // empty' <<<"$input")
    cwd=$(jq -r '.cwd // empty' <<<"$input")
    branch=$(branch_at "${cwd:-$project}") || exit 0

    if grep -Eq '(^|[;&|(]|\s)git(\s+-C\s+\S+)*\s+commit(\s|$)' <<<"$cmd" && is_protected "$branch"; then
      deny_protected "$branch" "Nothing is committed directly on it; branch protection would reject the push anyway."
    fi

    branch_re='git[[:space:]]+(switch[[:space:]]+(-c|-C|--create|--force-create)|checkout[[:space:]]+(-b|-B))[[:space:]]+([^[:space:];&|]+)([[:space:]]+([^-[:space:];&|][^[:space:];&|]*))?'
    if [[ $cmd =~ $branch_re ]]; then
      name=${BASH_REMATCH[4]//[\"\']/}
      start=${BASH_REMATCH[6]//[\"\']/}
      if grep -Eiq "$TICKET_KEY" <<<"$name"; then
        deny "Branch '$name' contains a ticket key. This repo keeps them out of branch names (the repo is public): use feature/<short-meaning>."
      fi
      if [ -n "$integration" ] && ! grep -Eq "$ALLOWED_PREFIXES" <<<"$name"; then
        deny "Branch '$name' doesn't follow this repo's naming ($flow). Use feature/<short-meaning>; the change type belongs in the commit title, not the branch name."
      fi
      from=${start:-$branch}
      if [ "${from#origin/}" != "$base_branch" ]; then
        deny "New branches start from '$base_branch' in this repo ($flow), but '$name' would start from '$from'. Run: $start_cmd"
      fi
      if [ "$from" = "$base_branch" ]; then
        behind=$(git -C "${cwd:-$project}" rev-list --count "$base_branch..origin/$base_branch" 2>/dev/null || echo 0)
        if [ "$behind" -gt 0 ]; then
          deny "Local '$base_branch' is $behind commit(s) behind origin/$base_branch, so '$name' would start from old code. Pull first: git switch $base_branch && git pull"
        fi
      fi
    fi
    ;;

  Agent)
    branch=$(branch_at "$project") || exit 0
    case "$(jq -r '.tool_input.subagent_type // empty' <<<"$input")" in
      senior-engineer)
        is_protected "$branch" && deny_protected "$branch" "Implementation runs on a work branch, not here."
        ;;
      implementation-planner|Plan)
        planning_context "$branch"
        ;;
    esac
    ;;

  EnterPlanMode)
    branch=$(branch_at "$project") || exit 0
    planning_context "$branch"
    ;;
esac

exit 0
