#!/usr/bin/env bash
# PreToolUse hook — deterministic enforcement for Bash commands
# Returns JSON in hookSpecificOutput format (Claude Code v2+)
#
# Four guards. The first two are unconditional; the last is conditional on
# repo state, because a destructive git op only destroys *uncommitted* work.

set -uo pipefail

INPUT=$(cat)

# jq, not grep: the old regex stopped at the first double quote, so any command
# containing one truncated what the guard could see. Verified bypass of the
# previous hook — guard saw only `echo \` here and allowed the force push:
#   echo "deploy" && git push --force origin main
COMMAND=$(printf '%s' "$INPUT" | jq -r '.tool_input.command // empty' 2>/dev/null)
[ -z "$COMMAND" ] && COMMAND="${1:-}"

DENY_REASON=""

if printf '%s' "$COMMAND" | grep -qE 'git[[:space:]]+push.*(origin|upstream)[[:space:]]+[^[:space:]]*(main|master)'; then
    DENY_REASON="Push to main/master is blocked. Use feature branches + PR."

# -f/--force must be a standalone flag. Anchoring on whitespace is what stops a
# branch name like `feature/tower-skill-tree-framework` from matching.
elif printf '%s' "$COMMAND" | grep -qE 'git[[:space:]]+push.*(--force([^-]|$)|[[:space:]]-f([[:space:]]|$))'; then
    DENY_REASON="Force push is blocked. Use --force-with-lease on a feature branch."

elif printf '%s' "$COMMAND" | grep -qE '\brm\b[^|;&]*[[:space:]]-[a-zA-Z]*[rR][a-zA-Z]*f|\brm\b[^|;&]*[[:space:]]-[a-zA-Z]*f[a-zA-Z]*[rR]'; then
    DENY_REASON="rm -rf is blocked. Use targeted file removal."

elif printf '%s' "$COMMAND" | grep -qE '\bsudo\b'; then
    DENY_REASON="sudo is blocked."

# Conditional: these are legitimate on a clean tree, and the operation only
# destroys uncommitted work — so gate on state, not on the command.
elif printf '%s' "$COMMAND" | grep -qE 'git[[:space:]]+(reset[[:space:]]+--hard|clean[[:space:]]+-[a-zA-Z]*f|checkout[[:space:]]+--[[:space:]]|restore)' &&
     [ -n "$(git status --porcelain 2>/dev/null)" ]; then
    DENY_REASON="Destructive git op with uncommitted changes. Commit, or 'git stash push -u' first."
fi

if [ -n "$DENY_REASON" ]; then
    printf '{"hookSpecificOutput": {"hookEventName": "PreToolUse", "permissionDecision": "deny", "permissionDecisionReason": "%s" }}\n' "$DENY_REASON"
else
    echo '{"hookSpecificOutput": {"hookEventName": "PreToolUse", "permissionDecision": "allow"}}'
fi
