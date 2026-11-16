#!/usr/bin/env bash
# PreToolUse: before Claude commits, flags changed files that docs/ or the README describe,
# so those docs get read against the diff before it lands.
set -uo pipefail

input=$(cat)
cmd=$(jq -r '.tool_input.command // empty' <<<"$input")
grep -Eq '(^|[;&|(]|\s)git(\s+-C\s+\S+)*\s+commit(\s|$)' <<<"$cmd" || exit 0
cwd=$(jq -r '.cwd // empty' <<<"$input")
cd "${cwd:-${CODEX_PROJECT_DIR:-.}}" 2>/dev/null || exit 0
top=$(git rev-parse --show-toplevel 2>/dev/null) || exit 0
cd "$top" || exit 0

# commit -a/--all also takes tracked changes that aren't staged yet.
all=""
grep -Eq '\scommit(\s.*)?\s(-[A-Za-z]*a[A-Za-z]*|--all)(\s|$)' <<<"$cmd" && all=1
changed=$({
  git diff --cached --name-only --diff-filter=ACMR
  [ -n "$all" ] && git diff --name-only --diff-filter=ACMR
} | sort -u)
[ -n "$changed" ] || exit 0

# A doc that is part of the commit has been looked at; every other doc, README included, still gets checked.
staged_docs=$(grep -E '^(docs/.*\.md|README\.md)$' <<<"$changed")

docs=()
while IFS= read -r doc; do
  grep -qxF -- "$doc" <<<"$staged_docs" || docs+=("$doc")
done < <(find docs -name '*.md' 2>/dev/null; [ -f README.md ] && echo README.md)
[ ${#docs[@]} -gt 0 ] || exit 0

# Docs name code types in backticks (`AggregateGate<T>`, `useTyping.jsx`), and config files by their
# file name (`docker-compose.yml`, Caddyfile, `.env.example`), so each kind is matched its own way.
hits=""
while IFS= read -r path; do
  file=$(basename "$path")
  case "$file" in
    *.md) continue ;;
    *.cs | *.js | *.jsx | *.ts | *.tsx)
      name=${file%.*}
      [ ${#name} -ge 4 ] || continue
      pattern="\`${name}([^A-Za-z0-9_]|\$)"
      ;;
    *)
      name=$file
      escaped=${file//./\\.}
      escaped=${escaped//+/\\+}
      pattern="(^|[^A-Za-z0-9_.-])${escaped}([^A-Za-z0-9_-]|\$)"
      ;;
  esac
  found=$(grep -lE "$pattern" "${docs[@]}" 2>/dev/null | paste -sd, - | sed 's/,/, /g')
  [ -n "$found" ] && hits+="- ${name} (${path}) -> ${found}"$'\n'
done <<<"$changed"
[ -n "$hits" ] || exit 0

# The same diff after a block means the docs were read and still match.
marker_dir="$(git rev-parse --git-dir)/codex-docs-reviewed"
digest=$({
  git diff --cached
  [ -n "$all" ] && git diff
} | sha1sum | cut -d' ' -f1)
[ -e "$marker_dir/$digest" ] && exit 0
mkdir -p "$marker_dir" && touch "$marker_dir/$digest"

reason="This commit changes files that these docs describe, and those docs aren't part of it:
${hits}Read those passages against the diff (git diff --cached). If the behaviour they describe changed, update the docs and stage them with the code. If they still match, tell the user so and run the same commit again; it passes for an unchanged diff."
jq -n --arg r "$reason" '{hookSpecificOutput: {hookEventName: "PreToolUse", permissionDecision: "deny", permissionDecisionReason: $r}}'
exit 0
