#!/usr/bin/env bash
# PreToolUse hook — deterministic enforcement for Bash commands
# Returns JSON in hookSpecificOutput format (Claude Code v2+)

INPUT=$(cat)

COMMAND=""
if echo "$INPUT" | grep -q '"command"'; then
    COMMAND=$(echo "$INPUT" | grep -o '"command"\s*:\s*"[^"]*"' | head -1 | sed 's/.*"command"\s*:\s*"//;s/"$//')
fi
if [ -z "$COMMAND" ] && [ -n "$1" ]; then
    COMMAND="$1"
fi

DENY_REASON=""

if echo "$COMMAND" | grep -qE 'git push.*(origin|upstream)\s+(main|master)'; then
    DENY_REASON="Push to main/master is blocked. Use feature branches + PR."
elif echo "$COMMAND" | grep -qE 'git push.*(-f|--force)'; then
    DENY_REASON="Force push is blocked."
elif echo "$COMMAND" | grep -qE '\brm\s+-rf\b'; then
    DENY_REASON="rm -rf is blocked. Use targeted file removal."
elif echo "$COMMAND" | grep -qE '\bsudo\b'; then
    DENY_REASON="sudo is blocked."
fi

if [ -n "$DENY_REASON" ]; then
    printf '{"hookSpecificOutput": {"hookEventName": "PreToolUse", "permissionDecision": "deny", "permissionDecisionReason": "%s" }}\n' "$DENY_REASON"
else
    echo '{"hookSpecificOutput": {"hookEventName": "PreToolUse", "permissionDecision": "allow"}}'
fi
