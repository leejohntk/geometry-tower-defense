#!/usr/bin/env bash
# PreToolUse hook — deterministic enforcement for Bash commands
# Returns JSON in hookSpecificOutput format (Claude Code v2+)
#
# Four guards. All but the last are unconditional; the destructive-git-op guard
# is conditional on repo state, because those ops only destroy *uncommitted*
# work.

set -uo pipefail

INPUT=$(cat)

# jq, not grep: the old regex stopped at the first double quote, so any command
# containing one truncated what the guard could see. Verified bypass of the
# previous hook — guard saw only `echo \` and allowed the force push.
COMMAND=$(printf '%s' "$INPUT" | jq -r '.tool_input.command // empty' 2>/dev/null)
[ -z "$COMMAND" ] && COMMAND="${1:-}"

# Match against the command with quoted spans blanked out. Text inside quotes is
# DATA — a commit message, an echo argument, a grep pattern — and never executes,
# so guarding it produces false positives: without this, committing a message
# that merely mentions a guarded pattern is denied (including the commit that
# added this file). Only quoted text is blanked, so a real command that simply
# echoes something first is still caught.
# `tr` first: sed is line-based, so without it a multi-line quoted block (the
# `git commit -m "$(cat <<'EOF' ... EOF)"` idiom) is never stripped and its body
# still trips the guards.
UNQUOTED=$(printf '%s' "$COMMAND" | tr '\n' ' ' | sed -E "s/'[^']*'/''/g; s/\"[^\"]*\"/\"\"/g")

DENY_REASON=""

if printf '%s' "$UNQUOTED" | grep -qE 'git[[:space:]]+push.*(origin|upstream)[[:space:]]+[^[:space:]]*(main|master)'; then
    DENY_REASON="Push to main/master is blocked. Use feature branches + PR."

# The force flag must be a standalone token. Anchoring on whitespace is what
# stops a branch name like `feature/tower-skill-tree-framework` from matching.
elif printf '%s' "$UNQUOTED" | grep -qE 'git[[:space:]]+push.*(--force([^-]|$)|[[:space:]]-f([[:space:]]|$))'; then
    DENY_REASON="Force push is blocked. Use --force-with-lease on a feature branch."

elif printf '%s' "$UNQUOTED" | grep -qE '\brm\b[^|;&]*[[:space:]]-[a-zA-Z]*[rR][a-zA-Z]*f|\brm\b[^|;&]*[[:space:]]-[a-zA-Z]*f[a-zA-Z]*[rR]'; then
    DENY_REASON="rm -rf is blocked. Use targeted file removal."

elif printf '%s' "$UNQUOTED" | grep -qE '\bsudo\b'; then
    DENY_REASON="sudo is blocked."

# Conditional: these are legitimate on a clean tree, and the operation only
# destroys uncommitted work — so gate on state, not on the command.
elif printf '%s' "$UNQUOTED" | grep -qE 'git[[:space:]]+(reset[[:space:]]+--hard|clean[[:space:]]+-[a-zA-Z]*f|checkout[[:space:]]+--[[:space:]]|restore)' &&
     [ -n "$(git status --porcelain 2>/dev/null)" ]; then
    DENY_REASON="Destructive git op with uncommitted changes. Commit, or 'git stash push -u' first."
fi

if [ -n "$DENY_REASON" ]; then
    printf '{"hookSpecificOutput": {"hookEventName": "PreToolUse", "permissionDecision": "deny", "permissionDecisionReason": "%s" }}\n' "$DENY_REASON"
else
    echo '{"hookSpecificOutput": {"hookEventName": "PreToolUse", "permissionDecision": "allow"}}'
fi
